using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.ReportingDtos;
using POS.Contract.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorBase.ERPFrontServices.PrintOrderServices;

public interface IPrintOrderService
{
    public Task PrintInitialDineInOrder(DineInOrderDetails order, 
        bool printCustomer = true, 
        bool printKitchen = true, 
        bool isClosing = false,
        bool isUpdate = false,
        bool isPrePrint = false,
        string? printerName = null,
        bool isCopy = false);

    public Task PrintDineInClosingReceipt(DineInOrderDetails orderId);

    public Task<bool> PrintTakeAwayOrder(decimal paid = 0, 
        string customerName = "", 
        string customerPhone = "",
        PaymentMethod paymentMethod = PaymentMethod.Cash);

    public Task<bool> PrintDeliveryOrder(decimal paid = 0);
    public Task<bool> ReprintOrderAsync(int orderId, bool isCopy = false, bool printCustomer = true, bool printKitchen = true, string? printerName = null);
    public Task PrintReceivedOrderAsync(OrderDto order);
    public Task PrintFollowUpOrderAsync(OrderDto order, List<TableItem>? addedItems = null);
    public Task PrintDispatchOrderAsync(OrderDto order);
    public Task PrintVoidReceiptAsync(OrderDto order, List<TableItem> voidedItems);
    public Task PrintDineInVoidReceiptAsync(DineInOrderDto order, List<TableItem> voidedItems);
    public Task PrintSalesSummaryAsync(SalesSummaryDto summary, List<SalesItemSummaryDto> items, string? printerName = null, bool useA4 = false, bool isArabic = true);
    public Task PrintEndDayReportAsync(SalesSummaryDto summary, List<SalesItemSummaryDto> items, string? printerName = null, bool useA4 = false, bool isArabic = true);
    public Task PrintDriverSettlementAsync(DriverSettlementDto settlement, DateTime posDate, string? printerName = null);
    public Task PrintAllDriversSettlementAsync(List<DriverSettlementDto> settlements, DateTime posDate, string? printerName = null);
    public Task PrintStaffPerformanceAsync(SalesSummaryDto summary, string? printerName = null, bool useA4 = false, bool isArabic = true, bool showOrders = false, string? specificStaffId = null);
    
    public Task<bool> PrintStaffMealOrder(string employeeName, List<TableItem> items);
    public Task<bool> PrintHospitalityOrder(string reason, List<TableItem> items);
}
