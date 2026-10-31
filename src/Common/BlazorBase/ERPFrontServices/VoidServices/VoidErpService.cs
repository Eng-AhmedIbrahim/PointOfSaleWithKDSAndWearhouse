using System.Net.Http.Json;
using BlazorBase.API;
using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.VoidDtos;

namespace BlazorBase.ERPFrontServices.VoidServices;

public class VoidErpService : IVoidErpService
{
    private readonly HttpClient _httpClient;

    public VoidErpService(IHttpClientFactory httpClientFactory, ApiSettings apiSettings)
    {
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<bool> VoidOrder(int orderId, string reason, string voidBy, string voidByName, bool returnToStock = false)
    {
        var response = await _httpClient.DeleteAsync($"/api/Void/voidOrder/{orderId}?reason={Uri.EscapeDataString(reason)}&voidBy={voidBy}&voidByName={Uri.EscapeDataString(voidByName)}&returnToStock={returnToStock}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> VoidItems(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName, bool returnToStock = false)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/Void/voidItems/{orderId}?reason={Uri.EscapeDataString(reason)}&voidBy={voidBy}&voidByName={Uri.EscapeDataString(voidByName)}&returnToStock={returnToStock}", itemsToVoid);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<VoidReportDto>> GetVoidReport(DateTime posDate)
    {
        var result = await _httpClient.GetFromJsonAsync<List<VoidReportDto>>($"/api/Void/report?posDate={posDate:yyyy-MM-dd}");
        return result ?? new List<VoidReportDto>();
    }
}
