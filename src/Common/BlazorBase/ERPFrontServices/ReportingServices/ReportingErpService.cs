using POS.Contract.Dtos.ReportingDtos;
using POS.Contract.Dtos.DineIn;
using System.Net.Http.Json;

namespace BlazorBase.ERPFrontServices.ReportingServices;

public class ReportingErpService : IReportingErpService
{
    private readonly HttpClient _httpClient;

    public ReportingErpService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SalesSummaryDto> GetSalesSummary(DateTime posDate, DateTime? endDate = null)
    {
        var url = $"api/reporting/sales-summary?posDate={posDate:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:yyyy-MM-dd}";
        var response = await _httpClient.GetFromJsonAsync<SalesSummaryDto>(url);
        return response ?? new SalesSummaryDto();
    }

    public async Task<List<AccountSummaryDto>> GetAccountsSummary(DateTime posDate, string staffType)
    {
        var response = await _httpClient.GetFromJsonAsync<List<AccountSummaryDto>>($"api/reporting/accounts-summary?posDate={posDate:yyyy-MM-dd}&staffType={staffType}");
        return response ?? new List<AccountSummaryDto>();
    }

    public async Task<List<OrderDto>> GetTodayOrders(DateTime posDate, string? orderType = null)
    {
        var url = $"api/reporting/today-orders?posDate={posDate:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(orderType)) url += $"&orderType={orderType}";
        
        var response = await _httpClient.GetFromJsonAsync<List<OrderDto>>(url);
        return response ?? new List<OrderDto>();
    }

    public async Task<List<OrderDto>> GetStaffOrders(DateTime posDate, string staffId, string staffType)
    {
        var url = $"api/reporting/staff-orders?posDate={posDate:yyyy-MM-dd}&staffId={staffId}&staffType={staffType}";
        var response = await _httpClient.GetFromJsonAsync<List<OrderDto>>(url);
        return response ?? new List<OrderDto>();
    }

    public async Task<List<SalesItemSummaryDto>> GetSalesItemsSummary(DateTime posDate, DateTime? endDate = null)
    {
        var url = $"api/reporting/sales-items-summary?posDate={posDate:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:yyyy-MM-dd}";
        var response = await _httpClient.GetFromJsonAsync<List<SalesItemSummaryDto>>(url);
        return response ?? new List<SalesItemSummaryDto>();
    }

    public async Task<List<OrderDto>> GetPendingOrders(DateTime posDate)
    {
        var response = await _httpClient.GetFromJsonAsync<List<OrderDto>>($"api/reporting/pending-orders?posDate={posDate:yyyy-MM-dd}");
        return response ?? new List<OrderDto>();
    }

    public async Task<bool> ResendOrderToBranch(int orderId)
    {
        var response = await _httpClient.PostAsync($"api/order/resendOrderToBranch/{orderId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<ReportResponseDto> GenerateReport(ReportRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/reports/generate", request);
        return await response.Content.ReadFromJsonAsync<ReportResponseDto>() ?? new ReportResponseDto();
    }
}
