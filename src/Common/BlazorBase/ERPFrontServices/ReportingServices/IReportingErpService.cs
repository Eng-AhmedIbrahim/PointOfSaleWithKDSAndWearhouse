using POS.Contract.Dtos.ReportingDtos;
using POS.Contract.Dtos.DineIn;

namespace BlazorBase.ERPFrontServices.ReportingServices;

public interface IReportingErpService
{
    Task<SalesSummaryDto> GetSalesSummary(DateTime posDate, DateTime? endDate = null);
    Task<List<AccountSummaryDto>> GetAccountsSummary(DateTime posDate, string staffType);
    Task<List<OrderDto>> GetTodayOrders(DateTime posDate, string? orderType = null);
    Task<List<OrderDto>> GetStaffOrders(DateTime posDate, string staffId, string staffType);
    Task<List<SalesItemSummaryDto>> GetSalesItemsSummary(DateTime posDate, DateTime? endDate = null);
    Task<List<OrderDto>> GetPendingOrders(DateTime posDate);
    Task<bool> ResendOrderToBranch(int orderId);
    Task<ReportResponseDto> GenerateReport(ReportRequestDto request);
}
