using Microsoft.EntityFrameworkCore;
using POS.Core.Services.Contract.InventoryServices;
using POS.Core.Entities.Item;
namespace POS.Services.DineInOrderServices;

public class DineInOrderService : IDineInOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderTrackService _orderTrackService;
    private readonly IAppDateService _appDateService;
    private readonly IInventoryService _inventoryService;

    public DineInOrderService(IUnitOfWork unitOfWork,
        IOrderTrackService orderTrackService,
        IAppDateService appDateService,
        IInventoryService inventoryService)
    {
        _unitOfWork = unitOfWork;
        _orderTrackService = orderTrackService;
        _appDateService = appDateService;
        _inventoryService = inventoryService;
    }

    private async Task RecalculateOrderTotalsAsync(Orders order, OrderSetting? settings = null)
    {
        if (settings == null)
        {
            settings = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(new POS.Core.Specifications.OrderSpecs.OrderSettingSpecs(OrderTypes.DineIn, order.MachineName));
        }

        if (order.OrderDetails == null) return;

        // 1. Filter active items - USE DISTINCT to prevent doubling if EF tracking has duplicate references
        var activeItems = order.OrderDetails
            .Where(i => i.Quantity > 0)
            .Distinct(ReferenceEqualityComparer.Instance)
            .Cast<OrderItemsDetails>()
            .ToList();

        if (!activeItems.Any())
        {
            order.Subtotal = 0;
            order.Tax = 0;
            order.Service = 0;
            order.Discount = 0;
            order.TotalDiscount = 0;
            order.GrandTotal = 0;
            order.Remain = 0;
            order.DiscountedItems = 0;
            return;
        }

        // 2. Calculate Subtotals (Including Attributes)
        decimal grossSubtotal = 0;
        decimal netSubtotal = 0;

        foreach (var item in activeItems)
        {
            decimal basePrice = item.UnitPrice ?? 0;
            decimal attributesPrice = item.OrderItemAttributes?.Sum(a => a.ExtraPrice ?? 0) ?? 0;
            decimal itemUnitPrice = basePrice + attributesPrice;
            decimal itemQty = item.Quantity ?? 0;

            decimal itemLineGross = Math.Round(itemQty * itemUnitPrice, 4);
            grossSubtotal += itemLineGross;

            // Sync TotalAmount to Gross for consistency across the system
            item.TotalAmount = Math.Round(itemLineGross, 2);

            // Use TotalAfterDiscount only if it represents a REAL discount (more than 0.01 difference)
            if (item.TotalAfterDiscount.HasValue && item.TotalAfterDiscount.Value > 0 &&
                (itemLineGross - item.TotalAfterDiscount.Value) >= 0.01m)
            {
                netSubtotal += item.TotalAfterDiscount.Value;
            }
            else
            {
                // No real discount, sync TotalAfterDiscount to NULL or Gross
                item.TotalAfterDiscount = null;
                netSubtotal += itemLineGross;
            }
        }

        order.Subtotal = Math.Round(netSubtotal, 2);

        decimal serviceRate = (order.WithoutService ?? false) ? 0 : (settings?.Service ?? 0);
        decimal taxRate = (order.WithoutTax ?? false) ? 0 : (settings?.Tax ?? 0);

        decimal serviceAmount = Math.Round((netSubtotal * serviceRate) / 100, 2);
        decimal taxAmount = Math.Round((netSubtotal * taxRate) / 100, 2);

        order.Service = serviceAmount;
        order.Tax = taxAmount;

        decimal interimTotal = Math.Round(netSubtotal + serviceAmount + taxAmount, 2);

        decimal orderDiscountAmount = 0;
        if (order.DiscountPercentage > 0)
        {
            orderDiscountAmount = Math.Round((interimTotal * (order.DiscountPercentage ?? 0)) / 100, 2);
            order.Discount = orderDiscountAmount;
        }
        else if (order.Discount > 0)
            orderDiscountAmount = Math.Round(order.Discount ?? 0, 2);

        decimal totalLineDiscount = Math.Max(0, grossSubtotal - netSubtotal);
        if (totalLineDiscount < 0.01m) totalLineDiscount = 0;

        order.TotalDiscount = Math.Round(orderDiscountAmount + totalLineDiscount, 2);
        order.DiscountedItems = Math.Round(totalLineDiscount, 2);

        string lineDiscountIndicator = "[خصم أصناف]";
        if (totalLineDiscount >= 0.01m)
        {
            if (string.IsNullOrEmpty(order.DiscountReason))
                order.DiscountReason = lineDiscountIndicator;
            else if (!order.DiscountReason.Contains(lineDiscountIndicator))
                order.DiscountReason = $"{lineDiscountIndicator} {order.DiscountReason}";
        }
        else if (!string.IsNullOrEmpty(order.DiscountReason))
            order.DiscountReason = (order.DiscountReason ?? "").Replace(lineDiscountIndicator, "").Trim();
        
        decimal totalBeforeRounding = interimTotal - orderDiscountAmount;
        order.GrandTotal = Math.Round(totalBeforeRounding * 2, MidpointRounding.AwayFromZero) / 2;
        order.Remain = Math.Max(0m, (order.GrandTotal ?? 0) - (order.Paid ?? 0));
    }

    private bool AreItemsSame(OrderItemsDetails item1, OrderItemsDetails item2)
    {
        if (item1.MenuSalesItemId != item2.MenuSalesItemId) return false;

        if (item1.UnitPrice != item2.UnitPrice) return false;

        var attrs1 = item1.OrderItemAttributes?.Select(a => a.AttributeItemId).OrderBy(id => id).ToList() ?? new List<int?>();
        var attrs2 = item2.OrderItemAttributes?.Select(a => a.AttributeItemId).OrderBy(id => id).ToList() ?? new List<int?>();

        if (attrs1.Count != attrs2.Count) return false;
        for (int i = 0; i < attrs1.Count; i++)
        {
            if (attrs1[i] != attrs2[i]) return false;

            var attr1 = item1.OrderItemAttributes!.First(a => a.AttributeItemId == attrs1[i]);
            var attr2 = item2.OrderItemAttributes!.First(a => a.AttributeItemId == attrs1[i]);
            if (attr1.ExtraPrice != attr2.ExtraPrice) return false;
        }

        return true;
    }

    public async Task<Orders?> CreateDineInOrderAsync(Orders order)
    {
        if (order is null)
            return null;

        // Allow multiple open orders for a table to support splitting
        // The check below is removed to allow creating multiple orders for the same table
        /*
        var existingOrder = await GetDineInOrderByTableIdAsync(order.TableID ?? 0, "Open");
        if (existingOrder != null)
        {
            Log.Warning("Attempted to create a duplicate DineIn order for Table {TableId}. Current OrderID: {OrderId}", order.TableID, order.OrderID);
            return existingOrder;
        }
        */

        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var appDate = await _appDateService.UpdateOrderNumber();
                    order.OrderID = appDate.CurrentOrderNumber;
                    order.OrderDate = appDate.PosDate.Date.Add(DateTime.Now.TimeOfDay);
                    order.OrderType = OrderTypes.DineIn;
                    order.OrderState = OrderStates.Pending;
                    if (string.IsNullOrEmpty(order.MachineName))
                    {
                        order.MachineName = Environment.MachineName;
                    }

                    // Apply captain tips deduction if enabled in settings
                    var dineInSettings = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(new POS.Core.Specifications.OrderSpecs.OrderSettingSpecs(OrderTypes.DineIn, order.MachineName));
                    if (dineInSettings != null && dineInSettings.DeductCaptainTips == true)
                    {
                        order.CaptainTipsDeduction = dineInSettings.CaptainTipsAmount;
                    }

                    await RecalculateOrderTotalsAsync(order, dineInSettings);

                    if (order.OrderDetails != null && order.OrderDetails.Any())
                    {
                        foreach (var item in order.OrderDetails)
                        {
                            item.OrderType = OrderTypes.DineIn;
                            item.MenuSalesItem = null; // Prevent identity conflict

                            // Ensure denormalized fields are populated
                            if (item.MenuSalesItemId.HasValue && (string.IsNullOrEmpty(item.ItemName) || (item.UnitPrice ?? 0) == 0))
                            {
                                var product = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(item.MenuSalesItemId.Value);
                                if (product != null)
                                {
                                    item.ItemName = string.IsNullOrEmpty(item.ItemName) ? product.EnglishName : item.ItemName;
                                    item.ItemNameAr = string.IsNullOrEmpty(item.ItemNameAr) ? product.ArabicName : item.ItemNameAr;
                                    item.UnitPrice = (item.UnitPrice ?? 0) == 0 ? product.Price : item.UnitPrice;
                                    item.CategoryId = item.CategoryId ?? product.CategoryId;
                                }
                            }

                            // Ensure OrderId is not set yet for new items to avoid confusion
                            item.OrderId = 0;

                            // Deduct from inventory
                            if (item.MenuSalesItemId.HasValue)
                            {
                                await _inventoryService.ConsumeItemStockAsync(
                                    item.MenuSalesItemId.Value,
                                    item.Quantity ?? 0,
                                    POS.Core.Entities.Item.TransactionType.Sale,
                                    order.OrderID.ToString());
                            }
                        }
                    }

                    // Add the whole order graph (order + details)
                    await _unitOfWork.Repository<Orders>().AddAsync(order);

                    var result = await _unitOfWork.CompleteAsync();
                    if (result <= 0)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    // Track the order creation
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = order.Id,
                        OrderType = "DineIn",
                        Action = "Created",
                        UserName = order.CashierName,
                        UserId = order.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = order.TableID,
                        TableName = order.TableName,
                        Details = $"Order created with {order.OrderDetails?.Count ?? 0} items. Total: {order.GrandTotal}",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return order;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "An error occurred while creating the DineIn order in Orders table.");
                    return null;
                }
            }
        });
    }

    public async Task<Orders?> UpdateDineInOrderAsync(Orders order)
    {
        if (order is null)
            return null;

        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    // Load existing order with OrderDetails to recalculate totals correctly
                    var spec = new DineInOrderByIdSpec(order.Id);
                    var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                    var existingOrder = orders.FirstOrDefault();

                    if (existingOrder is null)
                        return null;

                    // Clear navigation properties to avoid tracking conflicts
                    if (existingOrder.OrderDetails != null)
                    {
                        foreach (var item in existingOrder.OrderDetails)
                        {
                            if (item.MenuSalesItem != null)
                            {
                                item.MenuSalesItem.Category = null;
                                item.MenuSalesItem = null;
                            }
                        }
                    }

                    // Update order properties
                    existingOrder.Discount = order.Discount;
                    existingOrder.DiscountPercentage = order.DiscountPercentage;
                    existingOrder.DiscountType = order.DiscountType;
                    existingOrder.DiscountReason = order.DiscountReason;

                    // Recalculate totals based on OrderDetails and new discount
                    await RecalculateOrderTotalsAsync(existingOrder);

                    existingOrder.OrderNotice = order.OrderNotice;
                    existingOrder.CustomerName = order.CustomerName;
                    existingOrder.Phone1 = order.Phone1;
                    existingOrder.PaymentMethod = order.PaymentMethod;
                    existingOrder.CustomerCount = order.CustomerCount;
                    existingOrder.MaleCount = order.MaleCount;
                    existingOrder.FemaleCount = order.FemaleCount;
                    existingOrder.TableState = order.TableState;
                    existingOrder.WaiterID = order.WaiterID;
                    existingOrder.WaiterName = order.WaiterName;
                    existingOrder.PrintCount = 0; // Reset print count on any modification to force re-printing latest check

                    _unitOfWork.Repository<Orders>().Update(existingOrder);

                    var result = await _unitOfWork.CompleteAsync();
                    if (result <= 0)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    // Track the order update
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = order.Id,
                        OrderType = "DineIn",
                        Action = "Updated",
                        UserName = order.CashierName,
                        UserId = order.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = order.TableID,
                        TableName = order.TableName,
                        Details = $"Order updated. Total: {existingOrder.GrandTotal}",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return existingOrder;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "An error occurred while updating the DineIn order.");
                    return null;
                }
            }
        });
    }

    public async Task<Orders?> GetDineInOrderByIdAsync(int orderId)
    {
        var spec = new DineInOrderByIdSpec(orderId);
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return orders.FirstOrDefault();
    }

    public async Task<Orders?> GetDineInOrderByTableIdAsync(int tableId, string state = "Open")
    {
        var spec = new DineInOrderByTableIdSpec(tableId, state);
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return orders.OrderByDescending(o => o.OrderDate).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Orders>> GetOpenOrdersByTableIdAsync(int tableId)
    {
        var spec = new DineInOrderByTableIdSpec(tableId, "Open");
        return await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
    }

    public async Task<IReadOnlyList<Orders>> GetAllOpenDineInOrdersAsync()
    {
        var spec = new DineInOrderByStateSpec("Open");
        return await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
    }

    public async Task<bool> CloseDineInOrderAsync(int orderId, decimal? paid = null, decimal? remain = null)
    {
        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var order = await _unitOfWork.Repository<Orders>().GetByIdAsync(orderId);
                    if (order is null)
                        return false;

                    order.OrderState = OrderStates.Completed;
                    order.ClosingTime = DateTime.Now;

                    // Update payment information if provided
                    if (paid.HasValue)
                    {
                        order.Paid = paid.Value;
                    }

                    if (remain.HasValue)
                    {
                        order.Remain = remain.Value;
                    }
                    else if (paid.HasValue)
                    {
                        // Calculate Remain if Paid is provided but Remain is not
                        order.Remain = (order.GrandTotal ?? 0) - paid.Value;
                    }

                    order.TableState = "Closed";
                    _unitOfWork.Repository<Orders>().Update(order);

                    var result = await _unitOfWork.CompleteAsync();
                    if (result <= 0)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    // Track the order closure
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = order.OrderID,
                        OrderType = "DineIn",
                        Action = "Closed",
                        UserName = order.CashierName,
                        UserId = order.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = order.TableID,
                        TableName = order.TableName,
                        Details = $"Order closed. Total: {order.GrandTotal}, Paid: {order.Paid}, Remain: {order.Remain}",
                        ActionDateTime = DateTime.Now
                    });

                    if (order.TableID != null && order.TableID > 0)
                    {
                        // Check if there are other OPEN orders for this table
                        var openOrdersSpec = new BaseSpecifications<Orders>(o =>
                        o.TableID == order.TableID &&
                        o.OrderState == OrderStates.Pending &&
                        o.OrderID != orderId); // Exclude current order

                        var otherOpenOrdersCount = await _unitOfWork.Repository<Orders>().GetCountAsync(openOrdersSpec);

                        if (otherOpenOrdersCount == 0)
                        {
                            var table = await _unitOfWork.Repository<Table>().GetByIdAsync(order.TableID.Value);
                            if (table != null && table.TableState != TableState.Available)
                            {
                                table.TableState = TableState.Available;
                                _unitOfWork.Repository<Table>().Update(table);
                            }
                        }
                    }

                    await _unitOfWork.CompleteAsync(); // Save table changes
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "An error occurred while closing the DineIn order.");
                    return false;
                }
            }
        });
    }

    public async Task<bool> AddItemsToDineInOrderAsync(int orderId, List<OrderItemsDetails> items)
    {
        if (items is null || !items.Any())
            return false;

        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    decimal itemsTotal = 0;
                    var spec = new DineInOrderByIdSpec(orderId);
                    var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                    var order = orders.FirstOrDefault();

                    if (order is null)
                        return false;

                    // Clear navigation properties on all existing items to prevent tracking conflicts
                    if (order.OrderDetails != null)
                    {
                        foreach (var existingOrderItem in order.OrderDetails)
                        {
                            if (existingOrderItem.MenuSalesItem != null)
                            {
                                existingOrderItem.MenuSalesItem.Category = null;
                                existingOrderItem.MenuSalesItem = null;
                            }
                        }
                    }

                    foreach (var newItem in items)
                    {
                        newItem.OrderType = OrderTypes.DineIn;

                        // Ensure denormalized fields are populated
                        if (newItem.MenuSalesItemId.HasValue && (string.IsNullOrEmpty(newItem.ItemName) || (newItem.UnitPrice ?? 0) == 0))
                        {
                            var product = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(newItem.MenuSalesItemId.Value);
                            if (product != null)
                            {
                                newItem.ItemName = string.IsNullOrEmpty(newItem.ItemName) ? product.EnglishName : newItem.ItemName;
                                newItem.ItemNameAr = string.IsNullOrEmpty(newItem.ItemNameAr) ? product.ArabicName : newItem.ItemNameAr;
                                newItem.UnitPrice = (newItem.UnitPrice ?? 0) == 0 ? product.Price : newItem.UnitPrice;
                                newItem.CategoryId = newItem.CategoryId ?? product.CategoryId;
                            }
                        }

                        var existingItem = order.OrderDetails?.FirstOrDefault(i => AreItemsSame(i, newItem));
                        if (existingItem != null)
                        {
                            var oldQty = existingItem.Quantity ?? 0;
                            var newQty = oldQty + (newItem.Quantity ?? 0);

                            existingItem.Quantity = newQty;
                            existingItem.TotalAmount = (existingItem.TotalAmount ?? 0) + (newItem.TotalAmount ?? 0);

                            // Correctly merge TotalAfterDiscount (fallback to gross if null)
                            decimal currentNet = existingItem.TotalAfterDiscount ?? ((oldQty) * (existingItem.UnitPrice ?? 0));
                            decimal addedNet = newItem.TotalAfterDiscount ?? ((newItem.Quantity ?? 0) * (newItem.UnitPrice ?? 0));

                            // Only set TotalAfterDiscount if there's actually a discount involved, otherwise keep null for user preference
                            if (existingItem.TotalAfterDiscount != null || newItem.TotalAfterDiscount != null)
                            {
                                existingItem.TotalAfterDiscount = currentNet + addedNet;
                            }
                            else
                            {
                                existingItem.TotalAfterDiscount = null;
                            }

                            existingItem.TotalDiscountPrice = (existingItem.TotalDiscountPrice ?? 0) + (newItem.TotalDiscountPrice ?? 0);
                            existingItem.TotalDiscountAmount = (existingItem.TotalDiscountAmount ?? 0) + (newItem.TotalDiscountAmount ?? 0);

                            // Update denormalized data on existing item if missing
                            existingItem.ItemName ??= newItem.ItemName;
                            existingItem.ItemNameAr ??= newItem.ItemNameAr;
                            existingItem.UnitPrice ??= newItem.UnitPrice;

                            _unitOfWork.Repository<OrderItemsDetails>().Update(existingItem);
                        }
                        else
                        {
                            // Clear navigation properties on new item to prevent tracking conflicts
                            newItem.MenuSalesItem = null;
                            newItem.Order = null; // Don't link via navigation - use OrderId instead
                            newItem.OrderId = order.Id;
                            await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(newItem);

                            order.OrderDetails!.Add(newItem);
                        }

                        // Deduct from inventory for the new items (whether merged or new)
                        if (newItem.MenuSalesItemId.HasValue)
                        {
                            await _inventoryService.ConsumeItemStockAsync(
                                newItem.MenuSalesItemId.Value,
                                newItem.Quantity ?? 0,
                                POS.Core.Entities.Item.TransactionType.Sale,
                                order.OrderID.ToString());
                        }

                        itemsTotal += newItem.TotalAmount ?? 0;
                    }

                    // Update order totals using robust calculation
                    await RecalculateOrderTotalsAsync(order);
                    order.PrintCount = 0; // Reset on item addition

                    _unitOfWork.Repository<Orders>().Update(order);

                    var result = await _unitOfWork.CompleteAsync();
                    if (result <= 0)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    // Track the item addition
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = order.Id,
                        OrderType = "DineIn",
                        Action = "ItemsAdded",
                        UserName = order.CashierName,
                        UserId = order.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = order.TableID,
                        TableName = order.TableName,
                        Details = $"{items.Count} items added to order. Added Total: {itemsTotal}. New GrandTotal: {order.GrandTotal}",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "An error occurred while adding items to the DineIn order.");
                    return false;
                }
            }
        });
    }


    public async Task<bool> UpdateDineInOrderDiscountAsync(int orderId, decimal? discountAmount, decimal? discountPercentage, string? discountType, string? discountReason)
    {
        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    // Load order with OrderDetails to recalculate totals correctly
                    var spec = new DineInOrderByIdSpec(orderId);
                    var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                    var order = orders.FirstOrDefault();

                    if (order is null)
                        return false;

                    // Clear navigation properties to avoid tracking conflicts
                    if (order.OrderDetails != null)
                    {
                        foreach (var item in order.OrderDetails)
                        {
                            if (item.MenuSalesItem != null)
                            {
                                item.MenuSalesItem.Category = null;
                                item.MenuSalesItem = null;
                            }
                        }
                    }

                    order.Discount = discountAmount;
                    order.DiscountPercentage = discountPercentage;
                    order.DiscountType = discountType;
                    order.DiscountReason = discountReason;

                    await RecalculateOrderTotalsAsync(order);
                    order.PrintCount = 0; // Reset on discount change

                    _unitOfWork.Repository<Orders>().Update(order);

                    var result = await _unitOfWork.CompleteAsync();
                    if (result <= 0)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    // Track the discount update
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = order.Id,
                        OrderType = "DineIn",
                        Action = "DiscountApplied",
                        UserName = order.CashierName,
                        UserId = order.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = order.TableID,
                        TableName = order.TableName,
                        Details = $"Discount applied: {discountType} - Amount: {discountAmount}, Percentage: {discountPercentage}%, Reason: {discountReason}",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "An error occurred while updating discount for the DineIn order.");
                    return false;
                }
            }
        });
    }

    public async Task<bool> TransferDineInOrderAsync(int orderId, int newTableId, string newTableName)
    {
        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var order = await _unitOfWork.Repository<Orders>().GetByIdAsync(orderId);
                    if (order is null)
                        return false;

                    var oldTableName = order.TableName;
                    var oldTableId = order.TableID;

                    order.TableID = newTableId;
                    order.TableName = newTableName;

                    _unitOfWork.Repository<Orders>().Update(order);
                    await _unitOfWork.CompleteAsync();

                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = order.Id,
                        OrderType = "DineIn",
                        Action = "Transferred",
                        UserName = order.CashierName,
                        UserId = order.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = newTableId,
                        TableName = newTableName,
                        Details = $"Order transferred from Table {oldTableName} (ID: {oldTableId}) to Table {newTableName} (ID: {newTableId})",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "Error transferring DineIn order");
                    return false;
                }
            }
        });
    }

    public async Task<bool> MergeDineInOrdersAsync(int primaryOrderId, List<int> secondaryOrderIds)
    {
        if (secondaryOrderIds == null || !secondaryOrderIds.Any())
            return false;

        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var primaryOrderSpec = new DineInOrderByIdSpec(primaryOrderId);
                    var primaryOrders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(primaryOrderSpec);
                    var primaryOrder = primaryOrders.FirstOrDefault();

                    if (primaryOrder == null)
                        return false;

                    // Clear navigation properties immediately to avoid identity conflicts during EF tracking
                    if (primaryOrder.OrderDetails != null)
                    {
                        foreach (var item in primaryOrder.OrderDetails)
                        {
                            item.MenuSalesItem = null;
                            item.Order = null;
                        }
                    }

                    foreach (var secId in secondaryOrderIds)
                    {
                        var spec = new DineInOrderByIdSpec(secId);
                        var secOrders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                        var secOrder = secOrders.FirstOrDefault();

                        if (secOrder == null) continue;

                        // Clear navigation properties immediately to avoid identity conflicts
                        if (secOrder.OrderDetails != null)
                        {
                            foreach (var item in secOrder.OrderDetails)
                            {
                                item.MenuSalesItem = null;
                                item.Order = null;
                            }
                        }

                        // Merge Order-Level Discount
                        if ((primaryOrder.DiscountPercentage == null || primaryOrder.DiscountPercentage == 0) &&
                            (primaryOrder.Discount == null || primaryOrder.Discount == 0))
                        {
                            // Primary has no discount, take secondary's
                            primaryOrder.Discount = secOrder.Discount;
                            primaryOrder.DiscountPercentage = secOrder.DiscountPercentage;
                            primaryOrder.DiscountType = secOrder.DiscountType;
                            primaryOrder.DiscountReason = secOrder.DiscountReason;
                        }
                        else if (secOrder.Discount > 0 && (secOrder.DiscountPercentage == null || secOrder.DiscountPercentage == 0) &&
                                 (primaryOrder.DiscountPercentage == null || primaryOrder.DiscountPercentage == 0))
                        {
                            // Both are fixed amounts, sum them
                            primaryOrder.Discount = (primaryOrder.Discount ?? 0) + (secOrder.Discount ?? 0);
                            if (!string.IsNullOrEmpty(secOrder.DiscountReason))
                            {
                                primaryOrder.DiscountReason = string.IsNullOrEmpty(primaryOrder.DiscountReason)
                                    ? secOrder.DiscountReason
                                    : $"{primaryOrder.DiscountReason}, {secOrder.DiscountReason}";
                            }
                        }

                        // Move items or merge if same
                        if (secOrder.OrderDetails != null)
                        {
                            foreach (var secItem in secOrder.OrderDetails.ToList())
                            {
                                var primaryItem = primaryOrder.OrderDetails?.FirstOrDefault(i => AreItemsSame(i, secItem));

                                if (primaryItem != null)
                                {
                                    // Update existing item in primary order
                                    var oldQty = primaryItem.Quantity ?? 0;
                                    primaryItem.Quantity = oldQty + (secItem.Quantity ?? 0);

                                    // Merge financial fields
                                    // TotalAmount = Final Price, TotalAfterDiscount = Discounted Subtotal
                                    primaryItem.TotalAmount = (primaryItem.TotalAmount ?? 0) + (secItem.TotalAmount ?? 0);

                                    decimal primaryNet = primaryItem.TotalAfterDiscount ?? ((oldQty) * (primaryItem.UnitPrice ?? 0));
                                    decimal secNet = secItem.TotalAfterDiscount ?? ((secItem.Quantity ?? 0) * (secItem.UnitPrice ?? 0));

                                    if (primaryItem.TotalAfterDiscount != null || secItem.TotalAfterDiscount != null)
                                    {
                                        primaryItem.TotalAfterDiscount = primaryNet + secNet;
                                    }
                                    else
                                    {
                                        primaryItem.TotalAfterDiscount = null;
                                    }

                                    primaryItem.TotalDiscountAmount = (primaryItem.TotalDiscountAmount ?? 0) + (secItem.TotalDiscountAmount ?? 0);
                                    primaryItem.TotalDiscountPrice = (primaryItem.TotalDiscountPrice ?? 0) + (secItem.TotalDiscountPrice ?? 0);
                                    primaryItem.VoidAmount = (primaryItem.VoidAmount ?? 0) + (secItem.VoidAmount ?? 0); // Void quantity

                                    _unitOfWork.Repository<OrderItemsDetails>().Update(primaryItem);
                                }
                                else
                                {
                                    // Create a NEW item for the primary order (Deep Copy)
                                    var newItem = new OrderItemsDetails
                                    {
                                        OrderId = primaryOrderId,
                                        OrderType = OrderTypes.DineIn,
                                        MenuSalesItemId = secItem.MenuSalesItemId,
                                        // Do NOT set MenuSalesItem navigation property to avoid tracking conflict
                                        MenuSalesItem = null,

                                        ItemName = secItem.ItemName,
                                        ItemNameAr = secItem.ItemNameAr,
                                        CategoryId = secItem.CategoryId,
                                        CategoryName = secItem.CategoryName,
                                        UnitPrice = secItem.UnitPrice,
                                        Quantity = secItem.Quantity,

                                        TotalAmount = secItem.TotalAmount,
                                        TotalAfterDiscount = secItem.TotalAfterDiscount,

                                        Discount = secItem.Discount,
                                        TotalDiscountPrice = secItem.TotalDiscountPrice ?? 0,
                                        TotalDiscountPercentage = secItem.TotalDiscountPercentage,
                                        TotalDiscountAmount = secItem.TotalDiscountAmount ?? 0,

                                        IsVoided = secItem.IsVoided,
                                        VoidAmount = secItem.VoidAmount ?? 0,

                                        OrderItemAttributes = new List<OrderItemAttributes>(),
                                        OrderItemComments = new List<OrderItemComment>()
                                    };

                                    // Deep copy Attributes
                                    if (secItem.OrderItemAttributes != null)
                                    {
                                        foreach (var attr in secItem.OrderItemAttributes)
                                        {
                                            newItem.OrderItemAttributes.Add(new OrderItemAttributes
                                            {
                                                AttributeItemId = attr.AttributeItemId,
                                                AttributeName = attr.AttributeName,
                                                ExtraPrice = attr.ExtraPrice
                                            });
                                        }
                                    }

                                    // Deep copy Comments
                                    if (secItem.OrderItemComments != null)
                                    {
                                        foreach (var comment in secItem.OrderItemComments)
                                        {
                                            newItem.OrderItemComments.Add(new OrderItemComment
                                            {
                                                Comment = comment.Comment
                                            });
                                        }
                                    }

                                    // Add the NEW item to the repository
                                    await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(newItem);

                                    if (primaryOrder.OrderDetails == null) primaryOrder.OrderDetails = new List<OrderItemsDetails>();
                                    primaryOrder.OrderDetails.Add(newItem);
                                }
                            }
                        }

                        // Check if we need to free the table (if it's different from primary)
                        if (secOrder.TableID != null && secOrder.TableID > 0 && secOrder.TableID != primaryOrder.TableID)
                        {
                            var openOrdersSpec = new BaseSpecifications<Orders>(o =>
                                o.TableID == secOrder.TableID &&
                                o.OrderState == OrderStates.Pending &&
                                o.OrderID != secOrder.OrderID);

                            var otherOpenOrdersCount = await _unitOfWork.Repository<Orders>().GetCountAsync(openOrdersSpec);

                            if (otherOpenOrdersCount == 0)
                            {
                                var table = await _unitOfWork.Repository<Table>().GetByIdAsync(secOrder.TableID.Value);
                                if (table != null && table.TableState != TableState.Available)
                                {
                                    table.TableState = TableState.Available;
                                    _unitOfWork.Repository<Table>().Update(table);
                                }
                            }
                        }

                        // Track the deletion of the secondary order
                        await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                        {
                            OrderId = secOrder.OrderID,
                            OrderType = "DineIn",
                            Action = "MergedAndDeleted",
                            UserName = primaryOrder.CashierName,
                            UserId = primaryOrder.CashierID,
                            MachineName = Environment.MachineName,
                            TableId = secOrder.TableID,
                            TableName = secOrder.TableName,
                            Details = $"Order merged into Order ID {primaryOrder.OrderID} and deleted.",
                            ActionDateTime = DateTime.Now
                        });

                        // Remove secondary order completely (Cascade delete will handle old items)
                        _unitOfWork.Repository<Orders>().Delete(secOrder);
                    }

                    // Recalculate totals ONCE for the primary order after all secondary orders are merged
                    await RecalculateOrderTotalsAsync(primaryOrder);
                    primaryOrder.PrintCount = 0; // Reset on merge
                    _unitOfWork.Repository<Orders>().Update(primaryOrder);

                    await _unitOfWork.CompleteAsync();

                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = primaryOrder.OrderID,
                        OrderType = "DineIn",
                        Action = "Merged",
                        UserName = primaryOrder.CashierName,
                        UserId = primaryOrder.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = primaryOrder.TableID,
                        TableName = primaryOrder.TableName,
                        Details = $"Orders {string.Join(", ", secondaryOrderIds)} merged into this order. Final GrandTotal: {primaryOrder.GrandTotal}",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "Error merging DineIn orders");
                    return false;
                }
            }
        });
    }

    public async Task<bool> SplitDineInOrderAsync(int sourceOrderId, List<SplitTargetDto> targets)
    {
        if (targets == null || !targets.Any())
            return false;

        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var sourceOrderSpec = new DineInOrderByIdSpec(sourceOrderId);
                    var sourceOrders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(sourceOrderSpec);
                    var sourceOrder = sourceOrders.FirstOrDefault();

                    if (sourceOrder == null) return false;

                    decimal totalMovedFromSource = 0;

                    foreach (var targetDto in targets)
                    {
                        if (targetDto.ItemsToMove == null || !targetDto.ItemsToMove.Any()) continue;

                        // Get target table name
                        var targetTableResult = await _unitOfWork.Repository<Table>().GetByIdAsync(targetDto.TargetTableId);
                        if (targetTableResult == null) continue;

                        // Get a new OrderID for each split
                        var appDate = await _appDateService.UpdateOrderNumber();
                        int newOrderId = appDate.CurrentOrderNumber;

                        // Create target order (always new for splits as per requirements)
                        var targetOrder = new Orders
                        {
                            OrderDate = appDate.PosDate.Date.Add(DateTime.Now.TimeOfDay),
                            OrderID = newOrderId,
                            OrderType = OrderTypes.DineIn,
                            OrderState = OrderStates.Pending,
                            TableID = targetDto.TargetTableId,
                            TableName = string.IsNullOrEmpty(targetDto.Label)
                                        ? targetTableResult.TableName
                                        : $"{targetTableResult.TableName} - {targetDto.Label}",
                            BranchID = sourceOrder.BranchID,
                            BranchName = sourceOrder.BranchName,
                            CashierID = sourceOrder.CashierID,
                            CashierName = sourceOrder.CashierName,
                            GrandTotal = 0,
                            Subtotal = 0,
                            // Copy Captain Order from source order
                            WaiterID = sourceOrder.WaiterID,
                            WaiterName = sourceOrder.WaiterName,
                            OrderDetails = new List<OrderItemsDetails>()
                        };

                        await _unitOfWork.Repository<Orders>().AddAsync(targetOrder);
                        await _unitOfWork.CompleteAsync(); // Save to generate real Id

                        decimal targetMovedTotal = 0;

                        foreach (var splitRequest in targetDto.ItemsToMove)
                        {
                            var item = sourceOrder.OrderDetails?.FirstOrDefault(i => i.Id == splitRequest.OrderItemDetailId);
                            if (item == null || (item.Quantity ?? 0) < splitRequest.QuantityToMove) continue;

                            // Clear navigation properties on source item to prevent tracking conflicts
                            if (item.MenuSalesItem != null)
                            {
                                item.MenuSalesItem.Category = null;
                                item.MenuSalesItem = null;
                            }
                            item.Order = null;

                            // Use UnitPrice + Attributes as the absolute basis for value calculation
                            var basePrice = item.UnitPrice ?? 0;
                            var attributesPrice = item.OrderItemAttributes?.Sum(a => a.ExtraPrice ?? 0) ?? 0;
                            var itemUnitPrice = basePrice + attributesPrice;

                            var originalQty = (item.Quantity ?? 0) + splitRequest.QuantityToMove;
                            var valueToMove = itemUnitPrice * splitRequest.QuantityToMove;

                            // Handle discounts proportionally based on original net/gross ratio
                            decimal valueToMoveAfterDiscount = valueToMove;
                            if (item.TotalAfterDiscount.HasValue && item.TotalAfterDiscount.Value > 0 && originalQty > 0)
                            {
                                decimal lineDiscountRatio = item.TotalAfterDiscount.Value / (itemUnitPrice * originalQty);
                                valueToMoveAfterDiscount = Math.Round(valueToMove * lineDiscountRatio, 4);
                            }

                            // Update source item
                            item.Quantity -= splitRequest.QuantityToMove;
                            item.TotalAmount = Math.Round((item.Quantity ?? 0) * itemUnitPrice, 2);
                            item.TotalAfterDiscount = (item.TotalAfterDiscount.HasValue && item.TotalAfterDiscount > 0)
                                ? Math.Round(item.TotalAfterDiscount.Value - valueToMoveAfterDiscount, 2)
                                : null;

                            if (item.TotalAfterDiscount <= 0.01m) item.TotalAfterDiscount = null;
                            if (item.TotalAmount < 0) item.TotalAmount = 0;

                            if (item.Quantity <= 0)
                                _unitOfWork.Repository<OrderItemsDetails>().Delete(item);
                            else
                                _unitOfWork.Repository<OrderItemsDetails>().Update(item);

                            // Add to target
                            var newItem = new OrderItemsDetails
                            {
                                OrderType = OrderTypes.DineIn,
                                MenuSalesItemId = item.MenuSalesItemId,
                                ItemName = item.ItemName,
                                ItemNameAr = item.ItemNameAr,
                                CategoryId = item.CategoryId,
                                CategoryName = item.CategoryName,
                                UnitPrice = item.UnitPrice,
                                Quantity = splitRequest.QuantityToMove,
                                TotalAmount = Math.Round(valueToMove, 2),
                                TotalAfterDiscount = (valueToMoveAfterDiscount < valueToMove - 0.01m) ? Math.Round(valueToMoveAfterDiscount, 2) : null,
                                Discount = item.Discount,
                                TotalDiscountAmount = (originalQty > 0) ? (item.TotalDiscountAmount ?? 0) * (splitRequest.QuantityToMove / originalQty) : 0,
                                OrderItemAttributes = item.OrderItemAttributes?.Select(a => new OrderItemAttributes
                                {
                                    AttributeItemId = a.AttributeItemId,
                                    AttributeName = a.AttributeName,
                                    ExtraPrice = a.ExtraPrice
                                }).ToList()
                            };

                            // Subtract moved discount from source
                            item.TotalDiscountAmount = (item.TotalDiscountAmount ?? 0) - (newItem.TotalDiscountAmount ?? 0);
                            item.TotalDiscountPrice = (item.Quantity ?? 0) > 0 ? (item.TotalDiscountAmount / item.Quantity) : 0;

                            newItem.Order = targetOrder; // Link via navigation property
                            await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(newItem);

                            targetMovedTotal += valueToMove;
                        }

                        // Target order gets the same discount settings from source
                        targetOrder.DiscountPercentage = sourceOrder.DiscountPercentage;
                        targetOrder.DiscountType = sourceOrder.DiscountType;
                        targetOrder.DiscountReason = sourceOrder.DiscountReason;

                        // If source had a fixed discount, split it proportionally based on subtotal
                        if (sourceOrder.Discount > 0 && (sourceOrder.DiscountPercentage == null || sourceOrder.DiscountPercentage == 0))
                        {
                            decimal sourceOriginalSubtotal = (sourceOrder.Subtotal ?? 0) + totalMovedFromSource;
                            if (sourceOriginalSubtotal > 0)
                            {
                                decimal ratio = targetMovedTotal / sourceOriginalSubtotal;
                                targetOrder.Discount = Math.Round((sourceOrder.Discount ?? 0) * ratio, 2);

                                // Subtract from source
                                sourceOrder.Discount -= targetOrder.Discount;
                                if (sourceOrder.Discount < 0) sourceOrder.Discount = 0;
                            }
                        }

                        await RecalculateOrderTotalsAsync(targetOrder);
                        _unitOfWork.Repository<Orders>().Update(targetOrder);

                        totalMovedFromSource += targetMovedTotal;

                        // Track target order creation
                        await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                        {
                            OrderId = targetOrder.OrderID,
                            OrderType = "DineIn",
                            Action = "SplitCreated",
                            UserName = sourceOrder.CashierName,
                            UserId = sourceOrder.CashierID,
                            MachineName = Environment.MachineName,
                            TableId = targetOrder.TableID,
                            TableName = targetOrder.TableName,
                            Details = $"Order created via split from Order #{sourceOrder.OrderID}. Total: {targetOrder.GrandTotal}",
                            ActionDateTime = DateTime.Now
                        });
                    }

                    // Update source order totals
                    await RecalculateOrderTotalsAsync(sourceOrder);
                    sourceOrder.PrintCount = 0; // Reset on split

                    bool shouldVoid = sourceOrder.Subtotal <= 0 || (sourceOrder.OrderDetails != null && !sourceOrder.OrderDetails.Any(i => i.Quantity > 0));

                    if (shouldVoid)
                    {
                        sourceOrder.OrderState = OrderStates.Voided;
                        sourceOrder.VoidReason = "Completely split into other orders";
                        sourceOrder.VoidTime = DateTime.Now;
                        sourceOrder.VoidByName = sourceOrder.CashierName;
                    }

                    // Clear navigation property to prevent graph traversal issues during update
                    sourceOrder.OrderDetails = null;

                    _unitOfWork.Repository<Orders>().Update(sourceOrder);
                    await _unitOfWork.CompleteAsync();

                    // Track source order update
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = sourceOrder.OrderID,
                        OrderType = "DineIn",
                        Action = "Split",
                        UserName = sourceOrder.CashierName,
                        UserId = sourceOrder.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = sourceOrder.TableID,
                        TableName = sourceOrder.TableName,
                        Details = $"Split into {targets.Count} targets. Total moved: {totalMovedFromSource}",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "Error splitting DineIn order");
                    return false;
                }
            }
        });
    }


    public async Task<int> IncrementPrintCountAsync(int orderId)
    {
        try
        {
            var order = await _unitOfWork.Repository<Orders>().GetByIdAsync(orderId);

            if (order == null)
            {
                // Fallback: search by database Id if GetByIdAsync failed for some reason (though it shouldn't)
                var spec = new DineInOrderByIdSpec(orderId);
                var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                order = orders.FirstOrDefault();

                if (order != null)
                {
                    // Clear navigation properties to prevent Identity Resolution conflicts during Update
                    order.OrderDetails = null;
                }
            }

            if (order == null) return 0;

            order.PrintCount = (order.PrintCount ?? 0) + 1;
            _unitOfWork.Repository<Orders>().Update(order);
            await _unitOfWork.CompleteAsync();

            return order.PrintCount ?? 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for order {OrderId}", orderId);
            return 0;
        }
    }

    public async Task<bool> ReserveTableAsync(Orders reservationOrder)
    {
        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    if (reservationOrder == null || reservationOrder.TableID == null)
                        return false;

                    // Check if table already has a reservation or active order
                    var existingOrder = await GetDineInOrderByTableIdAsync(reservationOrder.TableID.Value, "Open");
                    if (existingOrder != null)
                    {
                        Log.Warning("Cannot reserve table {TableId} - already has an active order", reservationOrder.TableID);
                        return false;
                    }

                    var existingReservation = await GetDineInOrderByTableIdAsync(reservationOrder.TableID.Value, "Reserved");
                    if (existingReservation != null)
                    {
                        Log.Warning("Cannot reserve table {TableId} - already reserved", reservationOrder.TableID);
                        return false;
                    }

                    // Get app date for order number
                    var appDate = await _appDateService.UpdateOrderNumber();
                    reservationOrder.OrderID = appDate.CurrentOrderNumber;
                    reservationOrder.OrderDate = appDate.PosDate.Date.Add(DateTime.Now.TimeOfDay);
                    reservationOrder.OrderType = OrderTypes.DineIn;
                    reservationOrder.OrderState = OrderStates.Reserved;

                    if (string.IsNullOrEmpty(reservationOrder.MachineName))
                    {
                        reservationOrder.MachineName = Environment.MachineName;
                    }

                    // No items for reservation, just the booking
                    reservationOrder.OrderDetails = new List<OrderItemsDetails>();
                    reservationOrder.Subtotal = 0;
                    reservationOrder.Tax = 0;
                    reservationOrder.Service = 0;
                    reservationOrder.GrandTotal = reservationOrder.ReservationPaid ?? 0;

                    await _unitOfWork.Repository<Orders>().AddAsync(reservationOrder);

                    var result = await _unitOfWork.CompleteAsync();
                    if (result <= 0)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    // Create a record in the Reservations table as requested
                    var reservation = new POS.Core.Entities.ReservationEntity.Reservation
                    {
                        CustomerName = reservationOrder.ReservationCustomerName ?? reservationOrder.CustomerName ?? "",
                        PhoneNumber = reservationOrder.ReservationCustomerPhone ?? reservationOrder.Phone1 ?? "",
                        ReservationDate = reservationOrder.ScheduleDateTime ?? DateTime.Now,
                        GuestCount = reservationOrder.CustomerCount,
                        MaleCount = reservationOrder.MaleCount,
                        FemaleCount = reservationOrder.FemaleCount,
                        TableId = reservationOrder.TableID,
                        BranchId = reservationOrder.BranchID,
                        ReservationPaid = reservationOrder.ReservationPaid,
                        ReservationStatus = "Reserved",
                        Notes = reservationOrder.OrderNotice,
                        OrderId = reservationOrder.Id
                    };

                    await _unitOfWork.Repository<POS.Core.Entities.ReservationEntity.Reservation>().AddAsync(reservation);
                    await _unitOfWork.CompleteAsync();

                    // Link back to Order
                    reservationOrder.ReservationId = reservation.Id;
                    _unitOfWork.Repository<Orders>().Update(reservationOrder);
                    await _unitOfWork.CompleteAsync();

                    // Update table state to Reserved
                    var table = await _unitOfWork.Repository<Table>().GetByIdAsync(reservationOrder.TableID.Value);
                    if (table != null)
                    {
                        table.TableState = TableState.Reserved;
                        _unitOfWork.Repository<Table>().Update(table);
                        await _unitOfWork.CompleteAsync();
                    }

                    // Track the reservation
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = reservationOrder.Id,
                        OrderType = "DineIn",
                        Action = "Reserved",
                        UserName = reservationOrder.CashierName,
                        UserId = reservationOrder.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = reservationOrder.TableID,
                        TableName = reservationOrder.TableName,
                        Details = $"Table reserved for {reservationOrder.ReservationCustomerName} ({reservationOrder.ReservationCustomerPhone}). " +
                                  $"Scheduled: {reservationOrder.ScheduleDateTime}. Guests: {reservationOrder.CustomerCount}. " +
                                  $"Deposit: {reservationOrder.ReservationPaid}",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "Error creating table reservation");
                    return false;
                }
            }
        });
    }

    public async Task<bool> CancelReservationAsync(int orderId)
    {
        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var order = await _unitOfWork.Repository<Orders>().GetByIdAsync(orderId);
                    if (order == null)
                    {
                        // Try by database Id
                        var spec = new DineInOrderByIdSpec(orderId);
                        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                        order = orders.FirstOrDefault();
                    }

                    if (order == null)
                    {
                        // Last resort: Try by display OrderID if caller passed the sequence number (backward compatibility or search)
                        var lastSpec = new DineInOrderByIdSpec(orderId); // We assume orderId might be Id now
                        var lastOrders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(lastSpec);
                        order = lastOrders.FirstOrDefault();
                    }

                    if (order == null || order.OrderState != OrderStates.Reserved)
                    {
                        Log.Warning("Cannot cancel reservation - order {OrderId} not found or not reserved", orderId);
                        return false;
                    }

                    // Mark order as canceled
                    order.OrderState = OrderStates.Canceled;
                    order.ClosingTime = DateTime.Now;
                    _unitOfWork.Repository<Orders>().Update(order);

                    // Also cancel the linked reservation if it exists
                    if (order.ReservationId.HasValue)
                    {
                        var reservation = await _unitOfWork.Repository<POS.Core.Entities.ReservationEntity.Reservation>().GetByIdAsync(order.ReservationId.Value);
                        if (reservation != null)
                        {
                            reservation.ReservationStatus = "Cancelled";
                            _unitOfWork.Repository<POS.Core.Entities.ReservationEntity.Reservation>().Update(reservation);
                        }
                    }
                    else
                    {
                        // Search for reservation by OrderId if link is missing
                        var spec = new POS.Core.Specifications.ReservationSpecs.ReservationByOrderIdSpec(order.Id);
                        var reservations = await _unitOfWork.Repository<POS.Core.Entities.ReservationEntity.Reservation>().GetAllWithSpecificationAsync(spec);
                        var reservation = reservations.FirstOrDefault();
                        if (reservation != null)
                        {
                            reservation.ReservationStatus = "Cancelled";
                            _unitOfWork.Repository<POS.Core.Entities.ReservationEntity.Reservation>().Update(reservation);
                        }
                    }

                    var result = await _unitOfWork.CompleteAsync();
                    if (result <= 0)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    // Update table state to Available
                    if (order.TableID.HasValue)
                    {
                        var table = await _unitOfWork.Repository<Table>().GetByIdAsync(order.TableID.Value);
                        if (table != null)
                        {
                            table.TableState = TableState.Available;
                            _unitOfWork.Repository<Table>().Update(table);
                            await _unitOfWork.CompleteAsync();
                        }
                    }

                    // Track the cancellation
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = order.Id,
                        OrderType = "DineIn",
                        Action = "ReservationCanceled",
                        UserName = order.CashierName,
                        UserId = order.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = order.TableID,
                        TableName = order.TableName,
                        Details = $"Reservation canceled for {order.CustomerName}. Original schedule: {order.ScheduleDateTime}",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "Error canceling reservation for order {OrderId}", orderId);
                    return false;
                }
            }
        });
    }

    public async Task<bool> SeatReservationAsync(int orderId, string captainId, string captainName)
    {
        var strategy = _unitOfWork.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var order = await _unitOfWork.Repository<Orders>().GetByIdAsync(orderId);
                    if (order == null)
                    {
                        var spec = new DineInOrderByIdSpec(orderId);
                        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                        order = orders.FirstOrDefault();
                    }

                    if (order == null || order.OrderState != OrderStates.Reserved)
                    {
                        Log.Warning("Cannot seat reservation - order {OrderId} not found or not reserved", orderId);
                        return false;
                    }

                    // Mark order as Pending (Open)
                    order.OrderState = OrderStates.Pending;
                    if (!string.IsNullOrEmpty(captainId))
                    {
                        order.WaiterID = captainId;
                    }
                    order.WaiterName = captainName;

                    // Handle the deposit: Carry it over to the Paid field
                    // This ensures the remaining balance is correct when the order is closed
                    order.Paid = (order.Paid ?? 0) + (order.ReservationPaid ?? 0);
                    order.Remain = (order.GrandTotal ?? 0) - (order.Paid ?? 0);

                    _unitOfWork.Repository<Orders>().Update(order);

                    // Update the linked reservation record
                    if (order.ReservationId.HasValue)
                    {
                        var reservation = await _unitOfWork.Repository<POS.Core.Entities.ReservationEntity.Reservation>().GetByIdAsync(order.ReservationId.Value);
                        if (reservation != null)
                        {
                            reservation.ReservationStatus = "Seated";
                            _unitOfWork.Repository<POS.Core.Entities.ReservationEntity.Reservation>().Update(reservation);
                        }
                    }

                    // Update table state to Occupied
                    if (order.TableID.HasValue)
                    {
                        var table = await _unitOfWork.Repository<Table>().GetByIdAsync(order.TableID.Value);
                        if (table != null)
                        {
                            table.TableState = TableState.Occupied;
                            _unitOfWork.Repository<Table>().Update(table);
                        }
                    }

                    await _unitOfWork.CompleteAsync();

                    // Track the action
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = order.Id,
                        OrderType = "DineIn",
                        Action = "ReservationSeated",
                        UserName = order.CashierName,
                        UserId = order.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = order.TableID,
                        TableName = order.TableName,
                        Details = $"Reservation seated for {order.CustomerName}. Captain: {captainName}. Deposit {order.ReservationPaid} carried over to Paid.",
                        ActionDateTime = DateTime.Now
                    });

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error(ex, "Error seating reservation {OrderId}", orderId);
                    return false;
                }
            }
        });
    }
}
