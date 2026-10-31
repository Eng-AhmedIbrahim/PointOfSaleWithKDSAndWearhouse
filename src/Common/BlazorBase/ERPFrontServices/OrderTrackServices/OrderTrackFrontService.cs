namespace BlazorBase.ERPFrontServices.OrderTrackServices;

public class OrderTrackFrontService : IOrderTrackFrontService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<OrderTrackFrontService> _logger;

    public OrderTrackFrontService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<OrderTrackFrontService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
        _apiSettings = apiSettings;
        _logger = logger;
    }

    public async Task<bool> TrackOrderActionAsync(OrderTrackDto orderTrack)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.TrackOrderAction!, orderTrack);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking order action");
            return false;
        }
    }

    public async Task<IReadOnlyList<OrderTrackDto>> GetOrderTrackingHistoryAsync(int orderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetOrderTrackingHistory}/{orderId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get order tracking history");
                return new List<OrderTrackDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<List<OrderTrackDto>>(content) ?? new List<OrderTrackDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order tracking history");
            return new List<OrderTrackDto>();
        }
    }

    public async Task<IReadOnlyList<OrderTrackDto>> GetOrderTrackingByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetOrderTrackingByDateRange}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get order tracking by date range");
                return new List<OrderTrackDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            return ApiRequestHelpers.DeserializeResponseContent<List<OrderTrackDto>>(content) ?? new List<OrderTrackDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order tracking by date range");
            return new List<OrderTrackDto>();
        }
    }
}