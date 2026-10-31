using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.VoidDtos;
using POS.Core.Entities.DineIn;
using POS.Core.Entities.OrderEntity;
using POS.Core.Entities.Item;
using POS.Core.Repository.Contract;
using POS.Core.Services.Contract.VoidServices;
using POS.Core.Services.Contract.InventoryServices;
using POS.Core.Entities.Date;
using Serilog;

namespace POS.Services.VoidServices;

public class VoidService : IVoidService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;
    private readonly IRecipeService _recipeService;

    public VoidService(IUnitOfWork unitOfWork, IInventoryService inventoryService, IRecipeService recipeService)
    {
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _recipeService = recipeService;
    }

    public async Task<List<VoidReportDto>> GetVoidReportAsync(DateTime posDate)
    {
        var dateStart = posDate.Date;
        var dateEnd = posDate.Date.AddDays(1);

        // Get all orders that have any voiding records in history
        var voidSpec = new OrderVoidSpecs(dateStart, dateEnd);
        var voidSessions = (await _unitOfWork.Repository<OrderVoid>().GetAllWithSpecificationAsync(voidSpec))?.ToList() ?? new();
        
        // Also include orders marked voided or having voided amount on this date or created on this date
        var directVoidSpec = new BaseSpecifications<Orders>(o =>
            (o.OrderState == OrderStates.Voided || (o.TotalVoid.HasValue && o.TotalVoid > 0)) &&
            ((o.VoidTime.HasValue && o.VoidTime >= dateStart && o.VoidTime < dateEnd) ||
             (o.OrderDate.HasValue && o.OrderDate >= dateStart && o.OrderDate < dateEnd)));
        directVoidSpec.Includes.Add(o => o.OrderDetails!);
        var directVoidOrders = (await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(directVoidSpec))?.ToList() ?? new();

        var orderIds = voidSessions.Select(v => v.OrderId)
            .Union(directVoidOrders.Select(o => o.Id))
            .Distinct()
            .ToList();
        
        var ordersSpec = new BaseSpecifications<Orders>(o => orderIds.Contains(o.Id));
        ordersSpec.Includes.Add(o => o.OrderDetails!);
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(ordersSpec);

        var report = orders.Select(order =>
        {
            var sessions = voidSessions.Where(v => v.OrderId == order.Id).OrderBy(v => v.VoidDate).ToList();
            var lastSession = sessions.LastOrDefault();

            decimal voidedAmt = sessions.Any() ? sessions.Sum(s => s.TotalVoidedAmount) : (order.VoidAmount ?? order.TotalVoid ?? 0);
            decimal currentTotal = order.GrandTotal ?? 0;
            decimal originalTotal = (order.OrderState == OrderStates.Voided && (sessions.Any(s => s.IsFullVoid) || !sessions.Any()))
                ? (voidedAmt > 0 ? voidedAmt : currentTotal) 
                : currentTotal + voidedAmt;

            var voidedItems = (order.OrderDetails ?? new List<OrderItemsDetails>())
                .Where(d => (d.VoidAmount ?? 0) > 0 || d.IsVoided == true)
                .Select(d => new VoidItemReportDto
                {
                    OrderDetailId = d.Id,
                    ItemName = d.ItemName,
                    ItemNameAr = d.ItemNameAr,
                    CategoryName = d.CategoryName,
                    UnitPrice = d.UnitPrice,
                    VoidedQuantity = d.VoidAmount ?? 0,
                    RemainingQuantity = d.Quantity ?? 0,
                    VoidedValue = d.TotalVoidAmount,
                    IsFullyVoided = d.IsVoided == true,
                    // Note: These now represent the LATEST void info for this item
                    VoidByName = d.VoidByName ?? order.VoidByName,
                    VoidTime = d.VoidTime ?? order.VoidTime,
                    VoidReason = d.VoidReason ?? order.VoidReason
                }).ToList();

            return new VoidReportDto
            {
                OrderDbId = order.Id,
                OrderId = order.OrderID,
                OrderType = order.OrderType?.ToString(),
                OrderState = order.OrderState?.ToString(),
                OrderDate = order.OrderDate,
                CashierName = order.CashierName,
                CustomerName = order.CustomerName ?? order.TakeawayCustomerName,
                Phone = order.Phone1 ?? order.TakeawayCustomerPhone,
                TableId = order.TableID,
                TableName = order.TableName,
                WaiterName = order.WaiterName,
                DriverName = order.DriverName,
                BranchName = order.BranchName,
                BranchId = order.BranchID,
                VoidBy = lastSession?.VoidedBy ?? order.VoidBy,
                VoidByName = lastSession?.VoidedByName ?? order.VoidByName,
                VoidTime = lastSession?.VoidDate ?? order.VoidTime,
                VoidReason = lastSession?.Reason ?? order.VoidReason,
                VoidCount = order.VoidCount,
                OriginalTotal = originalTotal,
                VoidedAmount = voidedAmt,
                RemainingTotal = currentTotal,
                IsFullyVoided = order.OrderState == OrderStates.Voided,
                VoidedItems = voidedItems
            };
        }).OrderByDescending(r => r.VoidTime).ToList();

        return report;
    }

    public async Task<bool> VoidOrderAsync(int orderId, string reason, string voidBy, string voidByName, bool returnToStock = false)
    {
        using var transaction = _unitOfWork.BeginTransaction();
        try
        {
            var spec = new OrdersByIdSpecs(orderId);
            var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationTrackedAsync(spec);

            if (order is null) return false;

            var appDates = await _unitOfWork.Repository<AppDate>().GetAllAsync();
            var appDate = appDates?.FirstOrDefault();
            DateTime workingDate = (appDate != null && appDate.PosDate.Year > 2000) ? appDate.PosDate.Date : DateTime.Today;
            DateTime voidTime = workingDate.Add(DateTime.Now.TimeOfDay);

            // Create Void Header (History)
            var voidOperation = new OrderVoid
            {
                OrderId = order.Id,
                OrderType = order.OrderType ?? OrderTypes.TakeAway,
                OrderStateAtVoid = order.OrderState ?? OrderStates.Pending,
                VoidDate = voidTime,
                VoidedBy = voidBy,
                VoidedByName = voidByName,
                Reason = reason,
                IsFullVoid = true,
                SubtotalBefore = order.Subtotal ?? 0,
                TaxBefore = order.Tax ?? 0,
                ServiceBefore = order.Service ?? 0,
                DeliveryFeesBefore = order.DeliveryFees ?? 0,
                DiscountBefore = order.Discount ?? 0,
                GrandTotalBefore = order.GrandTotal ?? 0
            };

            decimal totalVoidedValue = 0;
            decimal totalVoidedCount = 0;

            if (order.OrderDetails != null)
            {
                foreach (var item in order.OrderDetails)
                {
                    if (item.IsVoided == true) continue;

                    decimal itemValue = item.TotalAmount ?? 0;
                    decimal itemQty = item.Quantity ?? 0;

                    var voidItem = new OrderVoidItem
                    {
                        OrderDetailId = item.Id,
                        QuantityBefore = itemQty,
                        QuantityVoided = itemQty,
                        QuantityAfter = 0,
                        AmountBefore = itemValue,
                        AmountVoided = itemValue,
                        AmountAfter = 0,
                        Reason = reason
                    };
                    voidOperation.VoidItems.Add(voidItem);

                    item.IsVoided = true;
                    item.VoidAmount = (item.VoidAmount ?? 0) + itemQty;
                    item.TotalVoidAmount = (item.TotalVoidAmount ?? 0) + itemValue;
                    item.VoidBy = voidBy;
                    item.VoidByName = voidByName;
                    item.VoidTime = voidTime;
                    item.VoidReason = reason;
                    item.Quantity = 0;
                    item.TotalAmount = 0;
                    item.TotalAfterDiscount = null;

                    totalVoidedValue += itemValue;
                    totalVoidedCount += itemQty;

                    // Inventory Management
                    if (item.MenuSalesItemId.HasValue)
                    {
                        if (returnToStock)
                        {
                            await _inventoryService.ConsumeItemStockAsync(
                                item.MenuSalesItemId.Value, 
                                itemQty, 
                                TransactionType.Void, 
                                order.OrderID.ToString());
                        }
                        else
                        {
                            // Record as Waste: Return then consume as Waste to document the audit trail without changing final quantity
                            await _inventoryService.ConsumeItemStockAsync(
                                item.MenuSalesItemId.Value, 
                                itemQty, 
                                TransactionType.Void, 
                                order.OrderID.ToString());
                                
                            await _inventoryService.ConsumeItemStockAsync(
                                item.MenuSalesItemId.Value, 
                                itemQty, 
                                TransactionType.Waste, 
                                order.OrderID.ToString());
                        }
                    }
                }
            }

            order.OrderState = OrderStates.Voided;
            order.TableState = "Closed";
            order.VoidTime = voidTime;
            order.VoidByName = voidByName;
            order.VoidBy = voidBy;
            order.VoidReason = reason;
            order.TotalVoid = (order.TotalVoid ?? 0) + totalVoidedValue;
            order.VoidCount = (order.VoidCount ?? 0) + totalVoidedCount;
            order.VoidAmount = (order.VoidAmount ?? 0) + totalVoidedValue;
            order.Subtotal = 0;
            order.GrandTotal = 0;
            order.Tax = 0;
            order.Service = 0;
            order.Paid = 0;
            order.Remain = 0;
            order.ClosingTime = voidTime;

            voidOperation.SubtotalAfter = 0;
            voidOperation.TaxAfter = 0;
            voidOperation.ServiceAfter = 0;
            voidOperation.GrandTotalAfter = 0;
            voidOperation.TotalVoidedAmount = totalVoidedValue;

            await _unitOfWork.Repository<OrderVoid>().AddAsync(voidOperation);

            // Table Release Logic
            if (order.OrderType == OrderTypes.DineIn && order.TableID.HasValue && order.TableID > 0)
            {
                var table = await _unitOfWork.Repository<Table>().GetByIdAsync(order.TableID.Value);
                if (table != null)
                {
                    var openOrdersSpec = new BaseSpecifications<Orders>(o => o.TableID == order.TableID && o.OrderState == OrderStates.Pending && o.Id != orderId);
                    var otherCount = await _unitOfWork.Repository<Orders>().GetCountAsync(openOrdersSpec);
                    if (otherCount == 0)
                    {
                        table.TableState = TableState.Available;
                        _unitOfWork.Repository<Table>().Update(table);
                    }
                }
            }

            await _unitOfWork.CompleteAsync();
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Log.Error(ex, "VoidOrderAsync: Error");
            return false;
        }
    }

    public async Task<bool> VoidItemsAsync(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName, bool returnToStock = false)
    {
        if (itemsToVoid == null || !itemsToVoid.Any()) return false;

        using var transaction = _unitOfWork.BeginTransaction();
        try
        {
            var spec = new OrdersByIdSpecs(orderId);
            var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationTrackedAsync(spec);
            if (order is null) return false;

            var appDates = await _unitOfWork.Repository<AppDate>().GetAllAsync();
            var appDate = appDates?.FirstOrDefault();
            DateTime workingDate = (appDate != null && appDate.PosDate.Year > 2000) ? appDate.PosDate.Date : DateTime.Today;
            DateTime voidTime = workingDate.Add(DateTime.Now.TimeOfDay);

            var voidOperation = new OrderVoid
            {
                OrderId = order.Id,
                OrderType = order.OrderType ?? OrderTypes.TakeAway,
                OrderStateAtVoid = order.OrderState ?? OrderStates.Pending,
                VoidDate = voidTime,
                VoidedBy = voidBy,
                VoidedByName = voidByName,
                Reason = reason,
                SubtotalBefore = order.Subtotal ?? 0,
                TaxBefore = order.Tax ?? 0,
                ServiceBefore = order.Service ?? 0,
                DeliveryFeesBefore = order.DeliveryFees ?? 0,
                DiscountBefore = order.Discount ?? 0,
                GrandTotalBefore = order.GrandTotal ?? 0
            };

            decimal voidedTotalValue = 0;
            decimal voidedTotalCount = 0;

            foreach (var voidRequest in itemsToVoid)
            {
                var item = order.OrderDetails?.FirstOrDefault(i => i.Id == voidRequest.OrderItemDetailId || i.MenuSalesItemId == voidRequest.OrderItemDetailId);
                if (item == null || voidRequest.QuantityToVoid <= 0 || item.Quantity < voidRequest.QuantityToVoid) continue;

                decimal qtyBefore = item.Quantity ?? 0;
                decimal unitPrice = qtyBefore > 0 ? (item.TotalAmount ?? 0) / qtyBefore : (item.UnitPrice ?? 0);
                decimal valToVoid = unitPrice * voidRequest.QuantityToVoid;

                var voidItem = new OrderVoidItem
                {
                    OrderDetailId = item.Id,
                    QuantityBefore = qtyBefore,
                    QuantityVoided = voidRequest.QuantityToVoid,
                    QuantityAfter = qtyBefore - voidRequest.QuantityToVoid,
                    AmountBefore = item.TotalAmount ?? 0,
                    AmountVoided = valToVoid,
                    AmountAfter = (item.TotalAmount ?? 0) - valToVoid,
                    Reason = reason
                };
                voidOperation.VoidItems.Add(voidItem);

                item.VoidAmount = (item.VoidAmount ?? 0) + voidRequest.QuantityToVoid;
                item.TotalVoidAmount = (item.TotalVoidAmount ?? 0) + valToVoid;
                item.VoidBy = voidBy;
                item.VoidByName = voidByName;
                item.VoidTime = voidTime;
                item.VoidReason = reason;
                item.Quantity -= voidRequest.QuantityToVoid;
                item.TotalAmount -= valToVoid;

                // Proportionally reduce TotalAfterDiscount if it exists
                if (item.TotalAfterDiscount.HasValue)
                {
                    decimal ratio = qtyBefore > 0 ? (qtyBefore - voidRequest.QuantityToVoid) / qtyBefore : 0;
                    item.TotalAfterDiscount = item.TotalAfterDiscount.Value * ratio;
                    
                    // Cleanup if it becomes 0
                    if (item.TotalAfterDiscount <= 0.001m)
                        item.TotalAfterDiscount = null;
                }

                if (voidRequest.QuantityToVoid > 0) item.IsVoided = true; 

                voidedTotalValue += valToVoid;
                voidedTotalCount += voidRequest.QuantityToVoid;

                // Inventory Management
                if (item.MenuSalesItemId.HasValue)
                {
                    // Inventory Management
                    if (item.MenuSalesItemId.HasValue)
                    {
                        if (returnToStock)
                        {
                            await _inventoryService.ConsumeItemStockAsync(
                                item.MenuSalesItemId.Value,
                                voidRequest.QuantityToVoid,
                                TransactionType.Void,
                                order.OrderID.ToString());
                        }
                        else
                        {
                            // Record as Waste: Return then consume as Waste to document the audit trail
                            await _inventoryService.ConsumeItemStockAsync(
                                item.MenuSalesItemId.Value,
                                voidRequest.QuantityToVoid,
                                TransactionType.Void,
                                order.OrderID.ToString());

                            await _inventoryService.ConsumeItemStockAsync(
                                item.MenuSalesItemId.Value,
                                voidRequest.QuantityToVoid,
                                TransactionType.Waste,
                                order.OrderID.ToString());
                        }
                    }
                }

            }

            if (voidedTotalCount == 0) return false;

            order.TotalVoid = (order.TotalVoid ?? 0) + voidedTotalValue;
            order.VoidCount = (order.VoidCount ?? 0) + voidedTotalCount;
            order.VoidAmount = (order.VoidAmount ?? 0) + voidedTotalValue;
            order.VoidTime = voidTime;
            order.VoidByName = voidByName;
            order.VoidBy = voidBy;
            order.VoidReason = reason;

            // Recalculate
            var activeItems = order.OrderDetails?.Where(i => (i.Quantity ?? 0) > 0).ToList() ?? new();
            if (!activeItems.Any())
            {
                order.OrderState = OrderStates.Voided;
                order.TableState = "Closed";
                order.Subtotal = 0; order.Tax = 0; order.Service = 0; order.GrandTotal = 0;
                voidOperation.IsFullVoid = true;
            }
            else
            {
                decimal oldGrandTotal = order.GrandTotal ?? 0;
                decimal subBefore = order.Subtotal ?? 1;
                decimal newSub = activeItems.Sum(i => i.TotalAmount ?? 0);
                
                // Reduce fixed discount proportionally if applicable
                if ((order.Discount ?? 0) > 0 && (order.DiscountPercentage == null || order.DiscountPercentage == 0))
                {
                    decimal ratio = subBefore > 0 ? (newSub / subBefore) : 0;
                    order.Discount = Math.Round((order.Discount ?? 0) * ratio, 2);
                }

                // Recalculate Tax and Service based on new subtotal
                decimal taxRatio = subBefore > 0 ? (newSub / subBefore) : 0;
                order.Tax = Math.Round((order.Tax ?? 0) * taxRatio, 2);
                order.Service = Math.Round((order.Service ?? 0) * taxRatio, 2);

                order.Subtotal = newSub;
                decimal calculatedGrandTotal = newSub + (order.Tax ?? 0) + (order.Service ?? 0) + (order.DeliveryFees ?? 0) - (order.Discount ?? 0);
                order.GrandTotal = Math.Round(calculatedGrandTotal * 2, MidpointRounding.AwayFromZero) / 2;

                decimal reduction = oldGrandTotal - (order.GrandTotal ?? 0);
                if (reduction > 0)
                {
                    order.Remain = (order.GrandTotal ?? 0) - (order.Paid ?? 0);
                    if (order.Remain < 0) order.Remain = 0;
                }
            }

            voidOperation.SubtotalAfter = order.Subtotal ?? 0;
            voidOperation.TaxAfter = order.Tax ?? 0;
            voidOperation.ServiceAfter = order.Service ?? 0;
            voidOperation.GrandTotalAfter = order.GrandTotal ?? 0;
            voidOperation.TotalVoidedAmount = voidedTotalValue;

            await _unitOfWork.Repository<OrderVoid>().AddAsync(voidOperation);
            await _unitOfWork.CompleteAsync();
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Log.Error(ex, "VoidItemsAsync: Error");
            return false;
        }
    }
}
