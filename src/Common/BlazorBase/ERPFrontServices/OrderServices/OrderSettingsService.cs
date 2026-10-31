using BlazorBase.API;
using BlazorBase.Helpers;
using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.KitchenDtos;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace BlazorBase.ERPFrontServices.OrderServices;
public class OrderSettingsService : IOrderSettingsService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<OrderSettingsService> _logger;

    public OrderSettingsService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<OrderSettingsService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<OrderDto?> CreateOrderAsync(OrderDto orderDto)
    {
        return await GetApiResponseAsync<OrderDto>(
            () => CreateOrderRequest(orderDto),
            "Failed to create order via the API."
        );
    }

    public async Task<OrderDto?> UpdateOrderAsync(OrderDto orderDto)
    {
        return await GetApiResponseAsync<OrderDto>(
            () => _httpClient.PutAsJsonAsync(_apiSettings.Endpoints!.CreateOrder!, orderDto),
            "Failed to update order via the API."
        );
    }

    public async Task<ICollection<OrderSettingToReturnDto>?> GetOrderSettingsAsync(string? computerName = null)
    {
        var url = _apiSettings.Endpoints!.GetOrderSettings;
        if (!string.IsNullOrEmpty(computerName))
        {
            url += $"?computerName={Uri.EscapeDataString(computerName)}";
        }

        return await GetApiResponseAsync<ICollection<OrderSettingToReturnDto>>(
            () => _httpClient.GetAsync(url!),
            "Failed to retrieve Order Settings from the API."
        );
    }

    public async Task<OrderSettingToReturnDto?> GetOrderSettingAsync(int orderType, string? computerName = null)
    {
        var url = _apiSettings.Endpoints!.GetOrderSettings!.Replace("GetOrderSettings", $"GetOrderSetting/{orderType}");
        if (!string.IsNullOrEmpty(computerName))
        {
            url += $"?computerName={Uri.EscapeDataString(computerName)}";
        }

        return await GetApiResponseAsync<OrderSettingToReturnDto>(
            () => _httpClient.GetAsync(url),
            $"Failed to retrieve Order Setting for orderType {orderType} from the API."
        );
    }

    public async Task<OrderSettingToReturnDto?> UpdateOrderSettingAsync(int orderType, OrderSettingToReturnDto dto, string? computerName = null)
    {
        var url = _apiSettings.Endpoints!.GetOrderSettings!.Replace("GetOrderSettings", $"UpdateOrderSetting/{orderType}");
        if (!string.IsNullOrEmpty(computerName))
        {
            url += $"?computerName={Uri.EscapeDataString(computerName)}";
        }

        return await GetApiResponseAsync<OrderSettingToReturnDto>(
            () => _httpClient.PutAsJsonAsync(url, dto),
            $"Failed to update Order Setting for orderType {orderType}."
        );
    }

    public async Task<List<POS.Contract.Dtos.AccountDtos.RoleToReturnDto>?> GetRolesAsync()
    {
        return await GetApiResponseAsync<List<POS.Contract.Dtos.AccountDtos.RoleToReturnDto>>(
            () => _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllRoles!),
            "Failed to retrieve roles."
        );
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
    {
        return await GetApiResponseAsync<OrderDto>(
            () => _httpClient.GetAsync($"api/Order/{orderId}"),
            "Failed to retrieve order."
        );
    }

    public async Task<int> IncrementPrintCountAsync(int orderId)
    {
        return await GetApiResponseAsync<int>(
            () => _httpClient.PutAsJsonAsync($"api/order/incrementPrintCount/{orderId}", new { }),
            "Failed to increment print count via the API."
        );
    }

    // Kitchen & Printer Management
    public async Task<List<KitchenTypeToReturnDto>?> GetAllKitchenTypesAsync()
    {
        return await GetApiResponseAsync<List<KitchenTypeToReturnDto>>(
            () => _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllKitchenTypes!),
            "Failed to retrieve kitchen types."
        );
    }

    public async Task<KitchenTypeToReturnDto?> CreateKitchenTypeAsync(KitchenTypeDto dto)
    {
        return await GetApiResponseAsync<KitchenTypeToReturnDto>(
            () => _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.GetAllKitchenTypes!, dto),
            "Failed to create kitchen type."
        );
    }

    public async Task<bool> UpdateKitchenTypeAsync(int id, KitchenTypeDto dto)
    {
        var url = $"{_apiSettings.Endpoints!.GetAllKitchenTypes}/{id}";
        var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PutAsJsonAsync(url, dto));
        return response?.IsSuccessStatusCode ?? false;
    }

    public async Task<bool> DeleteKitchenTypeAsync(int id)
    {
        var url = $"{_apiSettings.Endpoints!.GetAllKitchenTypes}/{id}";
        var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.DeleteAsync(url));
        return response?.IsSuccessStatusCode ?? false;
    }

    public async Task<List<KitchenPrintersToReturnDto>?> GetAllKitchenPrintersAsync()
    {
        return await GetApiResponseAsync<List<KitchenPrintersToReturnDto>>(
            () => _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllKitchenPrinters!),
            "Failed to retrieve kitchen printers."
        );
    }

    public async Task<KitchenPrintersToReturnDto?> CreateKitchenPrinterAsync(KitchenPrintersDto dto)
    {
        return await GetApiResponseAsync<KitchenPrintersToReturnDto>(
            () => _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.GetAllKitchenPrinters!, dto),
            "Failed to create kitchen printer."
        );
    }

    public async Task<bool> UpdateKitchenPrinterAsync(int id, KitchenPrintersDto dto)
    {
        var url = $"{_apiSettings.Endpoints!.GetAllKitchenPrinters}/{id}";
        var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PutAsJsonAsync(url, dto));
        return response?.IsSuccessStatusCode ?? false;
    }

    public async Task<bool> DeleteKitchenPrinterAsync(int id)
    {
        var url = $"{_apiSettings.Endpoints!.GetAllKitchenPrinters}/{id}";
        var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.DeleteAsync(url));
        return response?.IsSuccessStatusCode ?? false;
    }

    public async Task<List<string>?> GetInstalledPrintersAsync()
    {
        return await GetApiResponseAsync<List<string>>(
            () => _httpClient.GetAsync(_apiSettings.Endpoints!.GetInstalledPrinters!),
            "Failed to retrieve installed printers."
        );
    }

    private async Task<T> GetApiResponseAsync<T>(
        Func<Task<HttpResponseMessage>> apiRequest,
        string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest);
        if (response is null || !response.IsSuccessStatusCode)
        {
            var errorContent = response != null ? await response.Content.ReadAsStringAsync() : "No Response";
            _logger.LogError("API call failed: {ErrorMessage}. Status: {Status}, Content: {Content}", message ?? "No message provided.", response?.StatusCode, errorContent);
            
            // Attempt to extract the "message" if it's an ApiResponse JSON
            string userShownError = message ?? "Failed to communicate with API.";
            try 
            {
                var errorObj = ApiRequestHelpers.DeserializeResponseContent<BlazorBase.API.ApiResponse>(errorContent);
                if (!string.IsNullOrEmpty(errorObj?.Message)) 
                {
                    userShownError = errorObj.Message;
                }
            }
            catch 
            {
                if (!string.IsNullOrWhiteSpace(errorContent) && errorContent.Length < 200)
                    userShownError = errorContent;
            }

            throw new Exception(userShownError);
        }

        var content = await response.Content.ReadAsStringAsync();
        T? items = ApiRequestHelpers.DeserializeResponseContent<T>(content);

        return items!;
    }

    private Task<HttpResponseMessage> CreateOrderRequest(OrderDto orderDto)
      => _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateOrder!, orderDto);

    private Task<HttpResponseMessage> GetOrderSettingsRequest()
     => _httpClient.GetAsync(_apiSettings.Endpoints!.GetOrderSettings!);

}