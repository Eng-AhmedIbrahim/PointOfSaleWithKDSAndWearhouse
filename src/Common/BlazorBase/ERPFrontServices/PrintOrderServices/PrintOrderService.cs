using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.ReportingDtos;
using Serilog;

namespace BlazorBase.ERPFrontServices.PrintOrderServices;

public class PrintOrderService : IPrintOrderService
{
    private readonly CommonProperties _commonProperties;
    private readonly OrderSettingsService _orderSettingsService;

    public PrintOrderService(CommonProperties commonProperties,
        OrderSettingsService orderSettingsService)
    {
        _commonProperties = commonProperties;
        _orderSettingsService = orderSettingsService;
    }

    public async Task PrintInitialDineInOrder(DineInOrderDetails orderId, 
        bool printCustomer = true, 
        bool printKitchen = true, 
        bool isClosing = false, 
        bool isUpdate = false,
        bool isPrePrint = false,
        string? printerName = null,
        bool isCopy = false)
    {
        BackupMainOrderDtoDetails(null!, null!, null!);
        BackupDineInDate(orderId);
        
        // Populate the OrderDto with DineIn specifics
        _commonProperties.OrderDto!.OrderType = "DineIn";
        _commonProperties.OrderDto.OrderDetails = orderId.BasicOrderDetails?.Items;
        _commonProperties.OrderDto.SubTotal = orderId.BasicOrderDetails?.Account;
        _commonProperties.OrderDto.GrandTotal = orderId.BasicOrderDetails?.Total;
        _commonProperties.OrderDto.OrderId = orderId.BasicOrderDetails?.OrderId ?? _commonProperties.CurrentOrderId;
        _commonProperties.OrderDto.FooterMessage = _commonProperties.DineInSettings?.OrderStatment;
        _commonProperties.OrderDto.Tax = orderId.BasicOrderDetails?.Tax;
        _commonProperties.OrderDto.Services = orderId.BasicOrderDetails?.Service;

        // Guard: do not send empty order to API
        if (_commonProperties.OrderDto.OrderDetails == null || !_commonProperties.OrderDto.OrderDetails.Any())
        {
            Log.Warning("PrintInitialDineInOrder: OrderDetails is null or empty for OrderId={OrderId}. Skipping API call.", _commonProperties.OrderDto.OrderId);
            return;
        }

        await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);
    }

    private void BackupDineInDate(DineInOrderDetails orderId)
    {
        _commonProperties.OrderDto!.TakerID = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.TakerName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.TableId = orderId.RelatedTableId;
        _commonProperties.OrderDto.TableName = orderId.RelatedTableName;
        _commonProperties.OrderDto.WaiterId = orderId.CaptainId;
        _commonProperties.OrderDto.WaiterName = orderId.CaptainName;
    }

    public async Task<bool> PrintTakeAwayOrder(decimal paid = 0
        , string customerName = ""
        , string customerPhone = "", 
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        BackupMainOrderDtoDetails(customerName, customerPhone, paid, paymentMethod);

        // Guard: do not send empty order to API
        if (_commonProperties.OrderDto!.OrderDetails == null || !_commonProperties.OrderDto.OrderDetails.Any())
        {
            Log.Warning("PrintTakeAwayOrder: OrderDetails is null or empty. TableItems count={Count}. Skipping API call.",
                _commonProperties.TableItems?.Count ?? 0);
            return false;
        }

        var result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);
        if (result is null)
            return false;

        return true;
    }

    private void BackupMainOrderDtoDetails(string customerName, string customerPhone, decimal? paid = 0.00m, PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        _commonProperties.OrderDto!.OrderId = _commonProperties.CurrentOrderId;
        _commonProperties.OrderDto!.OrderType = _commonProperties.CurrentPosMode;
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.BranchId = _commonProperties.BranchDetails?.Id ?? 1;
        _commonProperties.OrderDto.BranchName = _commonProperties.BranchDetails?.Name;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        
        var settings = _commonProperties.CurrentPosMode switch
        {
            "TakeAway" => (dynamic?)_commonProperties.TakeAwaySettings,
            "DineIn" => (dynamic?)_commonProperties.DineInSettings,
            "Delivery" => (dynamic?)_commonProperties.DeliverySettings,
            _ => null
        };

        _commonProperties.OrderDto.FooterMessage = settings?.OrderStatment;
        _commonProperties.OrderDto.PaymentMethod = paymentMethod;
        _commonProperties.OrderDto.Paid = paid > 0.00m ? paid : (_commonProperties._financeSettingsList?.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0M);
        _commonProperties.OrderDto.Remaining = (_commonProperties._financeSettingsList?.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0M) - _commonProperties.OrderDto.Paid;
        _commonProperties.OrderDto.SubTotal = _commonProperties._financeSettingsList?.Count > 0 ? _commonProperties._financeSettingsList[0].Value : 0M;
        _commonProperties.OrderDto.Services = _commonProperties._financeSettingsList?.Count > 3 ? _commonProperties._financeSettingsList[3].Value : 0M;
        _commonProperties.OrderDto.Tax = _commonProperties._financeSettingsList?.Count > 2 ? _commonProperties._financeSettingsList[2].Value : 0M;
        _commonProperties.OrderDto.TotalOrderDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.GrandTotal = _commonProperties._financeSettingsList?.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0M;
        _commonProperties.OrderDto.OrderDate = _commonProperties.PosDate?.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));
        _commonProperties.OrderDto.OrderState = "Completed";
        _commonProperties.OrderDto.OrderNotice = _commonProperties.OrderNote;
        _commonProperties.OrderDto.OrderNotice = _commonProperties.OrderNote;
        
        // Combine Active and Voided items for saving
        var allItems = new List<TableItem>();
        if (_commonProperties.TableItems != null)
            allItems.AddRange(_commonProperties.TableItems);
        if (_commonProperties.VoidedTableItems != null)
            allItems.AddRange(_commonProperties.VoidedTableItems);
            
        _commonProperties.OrderDto.OrderDetails = allItems;

        // Calculate Total Void stats
        if (_commonProperties.VoidedTableItems != null && _commonProperties.VoidedTableItems.Any())
        {
             _commonProperties.OrderDto.VoidCount = _commonProperties.VoidedTableItems.Sum(i => i.VoidAmount ?? 0);
             _commonProperties.OrderDto.TotalVoid = _commonProperties.VoidedTableItems.Sum(i => i.TotalAmount ?? 0);
             _commonProperties.OrderDto.VoidAmount = _commonProperties.OrderDto.TotalVoid;
        }

        _commonProperties.OrderDto.CustomerName = customerName;
        _commonProperties.OrderDto.CustomerPhone = customerPhone;
        _commonProperties.OrderDto.OrderSettings = _commonProperties.OrderSettings;
        
        // Populate Discount Details
        if (_commonProperties.OrderDiscount != null)
        {
            _commonProperties.OrderDto.DiscountReason = _commonProperties.OrderDiscount.DiscountReason;
            _commonProperties.OrderDto.DiscountType = _commonProperties.OrderDiscount.DiscountType;
            _commonProperties.OrderDto.DiscountPercentage = _commonProperties.OrderDiscount.Percentage;
        }

        _commonProperties.OrderDto.TotalDiscount = GetTotalDiscountAmount();
        _commonProperties.OrderDto.DiscountBy = _commonProperties._financeSettingsList![1].Value != 0 ? _commonProperties.CurrentUserId : null;
        _commonProperties.OrderDto.DiscountByName = _commonProperties._financeSettingsList![1].Value != 0 ? _commonProperties.CurrentUser : null;
    }
    private decimal? GetTotalDiscountAmount()
    {
        if (string.IsNullOrEmpty(_commonProperties.OrderDto!.DiscountType))
            return null;

        if (_commonProperties.OrderDto!.DiscountType == "percentage")
            return _commonProperties._financeSettingsList![4].Value - (_commonProperties._financeSettingsList![4].Value * (_commonProperties._financeSettingsList![1].Value / 100));

        if (_commonProperties.OrderDto.DiscountType == "value")
            return _commonProperties._financeSettingsList![4].Value;

        return null;
    }

    public async Task<bool> ReprintOrderAsync(int orderId, bool isCopy = false, bool printCustomer = true, bool printKitchen = true, string? printerName = null)
    {
        // Currently just returning true to satisfy interface.
        // Logic can be expanded if needed for non-desktop scenarios.
        return await Task.FromResult(true);
    }

    public Task PrintDineInClosingReceipt(DineInOrderDetails orderId)
    {
        return Task.CompletedTask;
    }

    public Task PrintReceivedOrderAsync(OrderDto order)
    {
        return Task.CompletedTask;
    }

    public Task PrintFollowUpOrderAsync(OrderDto order, List<TableItem>? addedItems = null)
    {
        return Task.CompletedTask;
    }

    public Task PrintDispatchOrderAsync(OrderDto order)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> PrintDeliveryOrder(decimal paid = 0)
    {
        BackupMainOrderDtoDetails(
            _commonProperties.CustomerDetails?.CustomerName ?? "", 
            _commonProperties.CustomerDetails?.FirstPhoneNumber ?? "", 
            paid, 
            _commonProperties.SelectedPaymentMethod
        );
        
        // Add delivery specifics
        _commonProperties.OrderDto!.Phone2 = _commonProperties.CustomerDetails?.SecondPhoneNumber;
        _commonProperties.OrderDto.StreetName = _commonProperties.CustomerDetails?.ClientAddress;
        _commonProperties.OrderDto.ZoneBonus = _commonProperties.CustomerDetails?.ZoneFees ?? 0;
        _commonProperties.OrderDto.ZoneName = _commonProperties.CustomerDetails?.ZoneName;
        _commonProperties.OrderDto.AddressNotice = _commonProperties.CustomerDetails?.AddressNote;
        _commonProperties.OrderDto.HomeNum = _commonProperties.CustomerDetails?.HomeNumber;
        _commonProperties.OrderDto.FloorNum = _commonProperties.CustomerDetails?.FloorNumber;
        _commonProperties.OrderDto.ApartmentNum = _commonProperties.CustomerDetails?.FlatNumber;
        _commonProperties.OrderDto.ZoneID = _commonProperties.CustomerDetails?.ZoneID;
        _commonProperties.OrderDto.OrderState = "Pending";

        var result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);
        return result is not null;
    }

    public Task PrintVoidReceiptAsync(OrderDto order, List<TableItem> voidedItems)
    {
        // Default implementation does nothing or could be implemented if there's a server-side printing mechanism
        return Task.CompletedTask;
    }

    public Task PrintDineInVoidReceiptAsync(DineInOrderDto order, List<TableItem> voidedItems)
    {
        // Default implementation does nothing
        return Task.CompletedTask;
    }

    public Task PrintSalesSummaryAsync(SalesSummaryDto summary, List<SalesItemSummaryDto> items, string? printerName = null, bool useA4 = false, bool isArabic = true)
    {
        return Task.CompletedTask;
    }

    public Task PrintEndDayReportAsync(SalesSummaryDto summary, List<SalesItemSummaryDto> items, string? printerName = null, bool useA4 = false, bool isArabic = true)
    {
        return Task.CompletedTask;
    }

    public Task PrintDriverSettlementAsync(DriverSettlementDto settlement, DateTime posDate, string? printerName = null)
    {
        // Default no-op implementation for non-desktop scenarios
        return Task.CompletedTask;
    }

    public Task PrintAllDriversSettlementAsync(List<DriverSettlementDto> settlements, DateTime posDate, string? printerName = null)
    {
        // Default no-op implementation for non-desktop scenarios
        return Task.CompletedTask;
    }


    public Task PrintStaffPerformanceAsync(SalesSummaryDto summary, string? printerName = null, bool useA4 = false, bool isArabic = true, bool showOrders = false, string? specificStaffId = null)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> PrintStaffMealOrder(string employeeName, List<TableItem> items)
    {
        _commonProperties.OrderDto!.OrderId = _commonProperties.CurrentOrderId;
        _commonProperties.OrderDto.OrderType = "Staff";
        _commonProperties.OrderDto.OrderDetails = items;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.CustomerName = employeeName;
        _commonProperties.OrderDto.GrandTotal = 0;
        _commonProperties.OrderDto.SubTotal = 0;
        _commonProperties.OrderDto.OrderDate = _commonProperties.PosDate?.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));
        _commonProperties.OrderDto.OrderState = "Completed";

        var result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);
        return result is not null;
    }

    public async Task<bool> PrintHospitalityOrder(string hospitalityTitle, List<TableItem> items)
    {
        _commonProperties.OrderDto!.OrderId = _commonProperties.CurrentOrderId;
        _commonProperties.OrderDto.OrderType = "Hospitality";
        _commonProperties.OrderDto.OrderDetails = items;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.CustomerName = hospitalityTitle;
        _commonProperties.OrderDto.GrandTotal = 0;
        _commonProperties.OrderDto.SubTotal = 0;
        _commonProperties.OrderDto.OrderDate = _commonProperties.PosDate?.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));
        _commonProperties.OrderDto.OrderState = "Completed";

        var result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);
        return result is not null;
    }
}
