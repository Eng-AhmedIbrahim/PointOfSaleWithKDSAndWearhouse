using POS.Contract.Dtos.ReportingDtos;

namespace POS.Core.Services.Contract.ReportingServices;

public interface IReportingService
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime posDate, DateTime? endDate = null);
    Task<List<AccountSummaryDto>> GetAccountsSummaryAsync(DateTime posDate, string staffType);
    Task<List<OrderDto>> GetTodayOrdersAsync(DateTime posDate, string? orderType = null);
    Task<List<OrderDto>> GetStaffOrdersAsync(DateTime posDate, string staffId, string staffType);
    Task<List<SalesItemSummaryDto>> GetSalesItemsSummaryAsync(DateTime posDate, DateTime? endDate = null);
    Task<List<OrderDto>> GetPendingOrdersAsync(DateTime posDate);
}
