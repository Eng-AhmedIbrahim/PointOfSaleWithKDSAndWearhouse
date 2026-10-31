namespace POS.Services.OrderServices;
using POS.Core.Services.Contract.InventoryServices;
using POS.Core.Entities.Item;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppDateService _appDateService;
    private readonly IMapper _mapper;

    private readonly IInventoryService _inventoryService;
    private readonly IPosFeatureSettingsService _featureSettings;

    public OrderService(IUnitOfWork unitOfWork, 
        IAppDateService appDateService, 
        IMapper mapper,
        IInventoryService inventoryService,
        IPosFeatureSettingsService featureSettings)
    {
        _unitOfWork = unitOfWork;
        _appDateService = appDateService;
        _mapper = mapper;
        _inventoryService = inventoryService;
        _featureSettings = featureSettings;
    }
    public async Task<Orders?> CreateOrderAsync(Orders order)
    {
        if (order is null)
            return null;

        // Prevent duplicate creation for orders dispatched from the Call Center
        if (order.CallCenterOrderId.HasValue && order.CallCenterOrderId.Value > 0)
        {
            var existingOrder = await GetOrderByCallCenterIdAsync(order.CallCenterOrderId.Value);
            if (existingOrder != null)
            {
                Console.WriteLine($"[ORDER DEBUG] Order already exists for CallCenterOrderId: {order.CallCenterOrderId.Value}. Returning existing record.");
                return existingOrder;
            }
        }

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                Console.WriteLine($"[ORDER DEBUG] Starting CreateOrderAsync for OrderType: {order.OrderType}");
                var appDate = await _appDateService.UpdateOrderNumber();
                order.OrderID = appDate.CurrentOrderNumber;
                order.OrderDate = appDate.PosDate.Date.Add(DateTime.Now.TimeOfDay);

                if (order.OrderType == OrderTypes.TakeAway)
                {
                    if (order.TakeawayCustomer is not null)
                    {
                        var existingCustomer = await _unitOfWork.Repository<TakeawayCustomer>()
                            .GetByIdAsync(order.TakeawayCustomer.Id);

                        if (existingCustomer is null)
                        {
                            await _unitOfWork.Repository<TakeawayCustomer>().AddAsync(order.TakeawayCustomer);
                            await _unitOfWork.CompleteAsync();
                            order.TakeawayCustomerId = order.TakeawayCustomer.Id;
                        }
                        else
                        {
                            order.TakeawayCustomerId = existingCustomer.Id;
                        }
                    }
                    else
                    {
                        order.TakeawayCustomerId = null;
                    }
                }
                
                if (string.IsNullOrEmpty(order.MachineName))
                {
                    order.MachineName = Environment.MachineName;
                }

                // Ensure IDs are 0 for new order to avoid IDENTITY_INSERT errors
                order.Id = 0;
                if (order.OrderDetails != null)
                {
                    foreach (var item in order.OrderDetails)
                    {
                        item.Id = 0;
                        item.OrderId = 0; // Let EF handle the relationship
                        if (item.OrderType == 0 || (byte)item.OrderType == 0)
                        {
                            item.OrderType = order.OrderType ?? OrderTypes.Delivery;
                        }
                        
                        if (item.OrderItemAttributes != null)
                        {
                            foreach (var attr in item.OrderItemAttributes)
                            {
                                attr.Id = 0;
                                attr.OrderItemId = 0;
                            }
                        }
                    }
                }

                await _unitOfWork.Repository<Orders>().AddAsync(order);

                // Inventory deduction
                if (await _featureSettings.IsFeatureEnabledAsync("EnableInventoryTracking"))
                {
                    if (order.OrderDetails != null && order.OrderDetails.Any())
                    {
                        foreach (var item in order.OrderDetails)
                        {
                            if (item.MenuSalesItemId.HasValue)
                            {
                                await _inventoryService.ConsumeItemStockAsync(
                                    item.MenuSalesItemId.Value, 
                                    item.Quantity ?? 0, 
                                    TransactionType.Sale,
                                    order.OrderID.ToString());
                            }

                            // Deduct for Attributes if they are linked to menu items
                            if (item.OrderItemAttributes != null && item.OrderItemAttributes.Any())
                            {
                                foreach (var attr in item.OrderItemAttributes)
                                {
                                    if (attr.AttributeItemId.HasValue)
                                    {
                                        var attributeItem = await _unitOfWork.Repository<AttributeItem>().GetByIdAsync(attr.AttributeItemId.Value);
                                        if (attributeItem != null && attributeItem.RelatedMenuItemId > 0)
                                        {
                                            await _inventoryService.ConsumeItemStockAsync(
                                                attributeItem.RelatedMenuItemId,
                                                item.Quantity ?? 1, // Usually 1 attribute per item unless quantity is different
                                                TransactionType.Sale,
                                                order.OrderID.ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"[ORDER DEBUG] About to call final CompleteAsync for OrderID: {order.OrderID}");
                await _unitOfWork.CompleteAsync();
                
                // Entities are saved (either here or in inventory updates)
                transaction.Commit();
                Console.WriteLine($"[ORDER DEBUG] Transaction committed successfully for OrderID: {order.OrderID}");
                return order;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ORDER DEBUG] EXCEPTION in CreateOrderAsync: {ex.Message}");
                Console.WriteLine($"[ORDER DEBUG] StackTrace: {ex.StackTrace}");
                transaction.Rollback();
                Log.Error(ex, "An error occurred while creating the order.");
                return null;
            }
        }

    }

    public async Task<OrderSetting?> GetOrderSettingAsync(OrderTypes orderType, string? computerName = null)
    {
        var orderSettingSpecs = new OrderSettingSpecs(orderType, computerName);
        var settings = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(orderSettingSpecs);

        if (settings == null && !string.IsNullOrEmpty(computerName))
        {
            await InitializeOrderSettingsForComputerAsync(computerName);
            settings = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(orderSettingSpecs);
        }

        return settings;
    }

    public async Task<IReadOnlyList<OrderSetting>> GetOrderSettingsAsync(string? computerName = null)
    {
        if (string.IsNullOrEmpty(computerName))
            return await _unitOfWork.Repository<OrderSetting>().GetAllAsync();

        var spec = new OrderSettingSpecs(computerName);
        var settings = await _unitOfWork.Repository<OrderSetting>().GetAllWithSpecificationAsync(spec);

        if (settings == null || !settings.Any())
        {
            await InitializeOrderSettingsForComputerAsync(computerName);
            settings = await _unitOfWork.Repository<OrderSetting>().GetAllWithSpecificationAsync(spec);
        }

        return settings.ToList();
    }

    private async Task InitializeOrderSettingsForComputerAsync(string computerName)
    {
        try
        {
            // Get defaults (those with no ComputerName or marked as Default)
            var defaultSpec = new POS.Core.Specifications.BaseSpecifications<OrderSetting>(x => string.IsNullOrEmpty(x.ComputerName) || x.ComputerName == "Default");
            var defaultSettings = await _unitOfWork.Repository<OrderSetting>().GetAllWithSpecificationAsync(defaultSpec);

            if (defaultSettings == null || !defaultSettings.Any())
            {
                // Fallback to JSON if nothing in DB
                var jsonSettings = await PosDbContextDataSeed.GetDataFromJsonFile<OrderSetting>("orderSettings.json");
                if (jsonSettings != null && jsonSettings.Any())
                {
                    foreach (var setting in jsonSettings)
                    {
                        setting.Id = 0;
                        setting.ComputerName = computerName;
                        await _unitOfWork.Repository<OrderSetting>().AddAsync(setting);
                    }
                }
            }
            else
            {
                foreach (var setting in defaultSettings)
                {
                    var newSetting = new OrderSetting
                    {
                        BranchID = setting.BranchID,
                        OrderType = setting.OrderType,
                        OrderStatment = setting.OrderStatment,
                        Service = setting.Service,
                        Tax = setting.Tax,
                        Tips = setting.Tips,
                        JobID = setting.JobID,
                        CustomerReceiptCount = setting.CustomerReceiptCount,
                        FullKitchenReceiptCount = setting.FullKitchenReceiptCount,
                        SeparateReceiptCount = setting.SeparateReceiptCount,
                        ClosingReceiptCount = setting.ClosingReceiptCount,
                        AddServiceToItemPrice = setting.AddServiceToItemPrice,
                        CanCloseWithoutPrint = setting.CanCloseWithoutPrint,
                        DeductCaptainTips = setting.DeductCaptainTips,
                        CaptainTipsAmount = setting.CaptainTipsAmount,
                        ComputerName = computerName
                    };
                    await _unitOfWork.Repository<OrderSetting>().AddAsync(newSetting);
                }
            }
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing order settings for computer {ComputerName}", computerName);
        }
    }

    public async Task<OrderSetting?> UpdateOrderSettingAsync(OrderTypes orderType, OrderSetting orderSetting, string? computerName = null)
    {
        var orderSettingSpecs = new OrderSettingSpecs(orderType, computerName);
        var oldOrderSetting = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(orderSettingSpecs);

        if (oldOrderSetting is null)
            return null;

        oldOrderSetting.Service = orderSetting.Service;
        oldOrderSetting.Tips = orderSetting.Tips;
        oldOrderSetting.Tax = orderSetting.Tax;
        oldOrderSetting.OrderStatment = orderSetting.OrderStatment;
        oldOrderSetting.SeparateReceiptCount = orderSetting.SeparateReceiptCount;
        oldOrderSetting.CustomerReceiptCount = orderSetting.CustomerReceiptCount;
        oldOrderSetting.ClosingReceiptCount = orderSetting.ClosingReceiptCount;
        oldOrderSetting.FullKitchenReceiptCount = orderSetting.FullKitchenReceiptCount;
        oldOrderSetting.JobID = orderSetting.JobID;
        oldOrderSetting.AddServiceToItemPrice = orderSetting.AddServiceToItemPrice;
        oldOrderSetting.CanCloseWithoutPrint = orderSetting.CanCloseWithoutPrint;
        oldOrderSetting.DeductCaptainTips = orderSetting.DeductCaptainTips;
        oldOrderSetting.CaptainTipsAmount = orderSetting.CaptainTipsAmount;

        _unitOfWork.Repository<OrderSetting>().Update(oldOrderSetting);
        await _unitOfWork.CompleteAsync();
        return oldOrderSetting;
    }

    public async Task<Orders?> GetOrderByIdAsync(int id)
    {
        var spec = new OrdersByIdSpecs(id);
        return await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
    }

    public async Task<Orders?> GetOrderByCallCenterIdAsync(int callCenterOrderId)
    {
        var spec = new OrdersByCallCenterIdSpecs(callCenterOrderId);
        return await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
    }


    public async Task<bool> UpdateOrderStatusAsync(int id, OrderStates state)
    {
        // Use GetByIdAsync directly instead of specification to avoid tracking related entities like MenuSalesItems
        // which might already be tracked during the same request (e.g. in CreateOrderAsync)
        var order = await _unitOfWork.Repository<Orders>().GetByIdAsync(id);
        if (order == null) return false;

        order.OrderState = state;
        _unitOfWork.Repository<Orders>().Update(order);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<IReadOnlyList<Orders>?> GetFailedDeliveryOrdersAsync()
    {
        // Only retry orders that explicitly failed to deliver to branch
        var spec = new BaseSpecifications<Orders>(x => x.OrderType == OrderTypes.Delivery && 
            x.OrderState == OrderStates.FailedToDeliverToBranch);
        return await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
    }

    public async Task<OrderDto?> GetOrderDtoByIdAsync(int id)
    {
        var spec = new OrdersByIdSpecs(id);
        var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
        return order != null ? _mapper.Map<OrderDto>(order) : null;
    }

    public async Task<bool> UpdateOrderAsync(Orders order)
    {
        _unitOfWork.Repository<Orders>().Update(order);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<Orders?> FullUpdateOrderAsync(Orders order)
    {
        if (order is null || order.Id == 0)
            return null;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var spec = new OrdersByIdSpecs(order.Id);
                var existingOrder = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
                if (existingOrder == null) return null;

                // Update basic fields
                existingOrder.Subtotal = order.Subtotal;
                existingOrder.Service = order.Service;
                existingOrder.Tax = order.Tax;
                existingOrder.Discount = order.Discount;
                existingOrder.DiscountPercentage = order.DiscountPercentage;
                existingOrder.TotalDiscount = order.TotalDiscount;
                existingOrder.GrandTotal = order.GrandTotal;
                existingOrder.Paid = order.Paid;
                existingOrder.Remain = order.Remain;
                existingOrder.OrderNotice = order.OrderNotice;
                
                if (existingOrder.OrderState != OrderStates.Voided)
                    existingOrder.OrderState = order.OrderState;
                    
                existingOrder.PaymentMethod = order.PaymentMethod;
                
                existingOrder.CustomerName = order.CustomerName;
                existingOrder.Phone1 = order.Phone1;
                existingOrder.StreetName = order.StreetName;
                existingOrder.ZoneName = order.ZoneName;
                existingOrder.DeliveryFees = order.DeliveryFees;

                // Clear and Re-add details (simplest way to sync complex order graphs)
                if (existingOrder.OrderDetails != null)
                {
                    foreach (var detail in existingOrder.OrderDetails.ToList())
                    {
                        // Reverse inventory consumption
                        if (await _featureSettings.IsFeatureEnabledAsync("EnableInventoryTracking"))
                        {
                            if (detail.MenuSalesItemId.HasValue)
                            {
                                await _inventoryService.ConsumeItemStockAsync(
                                    detail.MenuSalesItemId.Value, 
                                    detail.Quantity ?? 0, 
                                    TransactionType.Void, 
                                    existingOrder.OrderID.ToString());
                            }

                            // Reverse attributes for deleted items
                            if (detail.OrderItemAttributes != null && detail.OrderItemAttributes.Any())
                            {
                                foreach (var attr in detail.OrderItemAttributes)
                                {
                                    if (attr.AttributeItemId.HasValue)
                                    {
                                        var attributeItem = await _unitOfWork.Repository<AttributeItem>().GetByIdAsync(attr.AttributeItemId.Value);
                                        if (attributeItem != null && attributeItem.RelatedMenuItemId > 0)
                                        {
                                            await _inventoryService.ConsumeItemStockAsync(
                                                attributeItem.RelatedMenuItemId,
                                                detail.Quantity ?? 1,
                                                TransactionType.Void,
                                                existingOrder.OrderID.ToString());
                                        }
                                    }
                                }
                            }
                        }

                        detail.MenuSalesItem = null;
                        detail.DineInOrder = null;
                        detail.Order = null;
                        
                        _unitOfWork.Repository<OrderItemsDetails>().Delete(detail);
                    }
                    await _unitOfWork.CompleteAsync();
                    existingOrder.OrderDetails.Clear();
                }

                if (order.OrderDetails != null)
                {
                    var mergedItems = order.OrderDetails
                        .GroupBy(i => new 
                        { 
                            i.MenuSalesItemId, 
                            i.IsVoided,
                            i.UnitPrice,
                            i.LineComment,
                            AttributeKey = string.Join(",", i.OrderItemAttributes?.Select(a => a.AttributeItemId).OrderBy(id => id) ?? Enumerable.Empty<int?>())
                        })
                        .Select(g => 
                        {
                            var first = g.First();
                            if (g.Count() > 1)
                            {
                                first.Quantity = g.Sum(x => x.Quantity ?? 0);
                                first.TotalAmount = g.Sum(x => x.TotalAmount ?? 0);
                                
                                var totalAds = g.Sum(x => x.TotalAfterDiscount ?? 0);
                                first.TotalAfterDiscount = g.Any(x => x.TotalAfterDiscount.HasValue) ? totalAds : null;

                                first.TotalDiscountAmount = g.Sum(x => x.TotalDiscountAmount ?? 0);
                                first.TotalVoidAmount = g.Sum(x => x.TotalVoidAmount ?? 0);
                                first.VoidAmount = g.Sum(x => x.VoidAmount ?? 0);
                                first.TotalDiscountPrice = g.Sum(x => x.TotalDiscountPrice ?? 0);
                            }
                            return first;
                        }).ToList();

                    foreach (var item in mergedItems)
                    {
                        item.OrderId = existingOrder.Id;
                        if (item.OrderType == 0 || (byte)item.OrderType == 0)
                        {
                            item.OrderType = existingOrder.OrderType ?? order.OrderType ?? OrderTypes.Delivery;
                        }
                        item.Id = 0; 
                        if (item.OrderItemAttributes != null)
                        {
                            foreach (var attr in item.OrderItemAttributes)
                            {
                                attr.Id = 0;
                                attr.OrderItemId = 0;
                            }
                        }
                        await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(item);
                        
                        // Consume inventory for new items
                        if (await _featureSettings.IsFeatureEnabledAsync("EnableInventoryTracking"))
                        {
                            if (item.MenuSalesItemId.HasValue)
                            {
                                await _inventoryService.ConsumeItemStockAsync(
                                    item.MenuSalesItemId.Value, 
                                    item.Quantity ?? 0, 
                                    TransactionType.Sale, 
                                    existingOrder.OrderID.ToString());
                            }

                            // Deduct for Attributes in full update
                            if (item.OrderItemAttributes != null && item.OrderItemAttributes.Any())
                            {
                                foreach (var attr in item.OrderItemAttributes)
                                {
                                    if (attr.AttributeItemId.HasValue)
                                    {
                                        var attributeItem = await _unitOfWork.Repository<AttributeItem>().GetByIdAsync(attr.AttributeItemId.Value);
                                        if (attributeItem != null && attributeItem.RelatedMenuItemId > 0)
                                        {
                                            await _inventoryService.ConsumeItemStockAsync(
                                                attributeItem.RelatedMenuItemId,
                                                item.Quantity ?? 1,
                                                TransactionType.Sale,
                                                existingOrder.OrderID.ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                _unitOfWork.Repository<Orders>().Update(existingOrder);
                await _unitOfWork.CompleteAsync();

                transaction.Commit();
                return await GetOrderByIdAsync(existingOrder.Id);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while updating the order {Id}.", order.Id);
                return null;
            }
        }
    }
    public async Task<IReadOnlyList<Orders>> GetOrdersByCustomerPhoneAsync(string phoneNumber)
    {
        var spec = new OrdersByCustomerPhoneSpecs(phoneNumber);
        return await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
    }

    public async Task<int> IncrementPrintCountAsync(int id)
    {
        try
        {
            var spec = new OrdersByIdSpecs(id);
            var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
            
            if (order == null) return 0;

            order.PrintCount = (order.PrintCount ?? 0) + 1;
            
            _unitOfWork.Repository<Orders>().Update(order);
            await _unitOfWork.CompleteAsync();
            
            return order.PrintCount ?? 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for order {Id}", id);
            return 0;
        }
    }
}