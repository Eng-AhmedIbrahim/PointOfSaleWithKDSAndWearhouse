using BlazorBase.ERPFrontServices.DineInServices;
using POS.Contract.Dtos.DineIn;

namespace BlazorBase.ERPFrontServices.DistributionServices;

public class DistributionErpService : IDistributionErpService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<DineInService> _logger;

    public DistributionErpService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<DineInService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(_apiSettings!.ApiName!);
    }

    public async Task<ICollection<UserToReturnDto>> GetDeliveryUsers()
    => await GetApiResponseAsync<UserToReturnDto>(GetDeliveryUsersRequest,
         "Failed to retrieve Delivery Users from the API.");

    private async Task<HttpResponseMessage> GetDeliveryUsersRequest()
        => await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetUsersByRole}/17");

    public async Task<ICollection<OrderDto>> GetUnCompletedDeliveryOrders()
   => await GetApiResponseAsync<OrderDto>(GetUnCompletedDeliveryOrdersRequest,
        "Failed to retrieve UnCompleted Delivery Orders from the API.");
    private async Task<HttpResponseMessage> GetUnCompletedDeliveryOrdersRequest()
      => await _httpClient.GetAsync(_apiSettings.Endpoints!.GetUnCompletedDeliveryOrders);


    public async Task<OrderDto?> DispatchOrder(OrderDto orderDto)
    {
        return await GetSingleApiResponseAsync<OrderDto>(
            () => GetDispatchOrderRequest(orderDto),
            "Failed to dispatch order from the API.");
    }

    private async Task<HttpResponseMessage> GetDispatchOrderRequest(OrderDto orderDto)
    {
        return await _httpClient.PutAsJsonAsync(
            _apiSettings.Endpoints!.DispatchOrder,
            orderDto);
    }
    public async Task<OrderDto?> CollectDeliveryOrder(OrderDto orderDto)
    {
        return await GetSingleApiResponseAsync<OrderDto>(
       () => CollectDeliveryOrderRequest(orderDto),
       "Failed to Update order State from the API.");
    }

    private async Task<HttpResponseMessage> CollectDeliveryOrderRequest(OrderDto orderDto)
    {
        return await _httpClient.PutAsJsonAsync(
            _apiSettings.Endpoints!.CollectDelivery,
            orderDto);
    }

    public async Task<bool> UnDispatchOrder(int orderId)
    {
        var response = await _httpClient.PutAsync($"{_apiSettings.Endpoints!.UnDispatchOrder}/{orderId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CollectDriverOrders(string driverId)
    {
        var response = await _httpClient.PutAsync($"{_apiSettings.Endpoints!.CollectDriverOrders}/{driverId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CollectAllOrders()
    {
        var response = await _httpClient.PutAsync(_apiSettings.Endpoints!.CollectAllOrders, null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<DriverSettlementDto>> GetDriverSettlement(DateTime posDate)
    {
        var endpoint = _apiSettings.Endpoints?.GetDriverSettlement ?? "api/Distribution/driver-settlement";
        var response = await _httpClient.GetAsync($"{endpoint}?posDate={posDate:yyyy-MM-dd}");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<DriverSettlementDto>>() ?? new();
        }
        return new();
    }

    public async Task<bool> VoidOrder(int orderId, string reason, string voidBy, string voidByName)
    {
        var response = await _httpClient.DeleteAsync($"/api/Distribution/voidOrder/{orderId}?reason={Uri.EscapeDataString(reason)}&voidBy={voidBy}&voidByName={Uri.EscapeDataString(voidByName)}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> VoidItems(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/Distribution/voidItems/{orderId}?reason={Uri.EscapeDataString(reason)}&voidBy={voidBy}&voidByName={Uri.EscapeDataString(voidByName)}", itemsToVoid);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<OrderDto>> GetVoidedOrders(DateTime posDate)
    {
        var response = await _httpClient.GetAsync($"/api/Distribution/voided-orders?posDate={posDate:yyyy-MM-dd}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<OrderDto>>() ?? new();
        }
        return new();
    }

    private async Task<ICollection<T>> GetApiResponseAsync<T>(
      Func<Task<HttpResponseMessage>> apiRequest,
      string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest);
        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("API call failed: {ErrorMessage}", message ?? "No message provided.");
            return [];
        }

        var content = await response.Content.ReadAsStringAsync();
        var items = ApiRequestHelpers.DeserializeResponseContent<List<T>>(content);

        return items ?? [];
    }

    private async Task<T> GetSingleApiResponseAsync<T>(
     Func<Task<HttpResponseMessage>> apiRequest,
     string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest);
        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("API call failed: {ErrorMessage}", message ?? "No message provided.");
            return default!;
        }

        var content = await response.Content.ReadAsStringAsync();
        var items = ApiRequestHelpers.DeserializeResponseContent<T>(content);

        return items ?? default!;
    }
}