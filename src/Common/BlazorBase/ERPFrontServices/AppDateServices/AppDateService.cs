namespace BlazorBase.ERPFrontServices.AppDateServices;

public class AppDateService : IAppDateService
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<AppDateService> _logger;
    private readonly HttpClient _httpClient;

    public AppDateService(ApiSettings apiSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<AppDateService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(_apiSettings.ApiName!);
    }

    public async Task<AppDateToReturnDto> UpdateOrderCount()
    {
        return await GetApiResponseAsync<AppDateToReturnDto>(
             UpdateOrderCountRequest,
            "Failed to update order count.");
    }

    public async Task<AppDateToReturnDto> GetAppDate()
    {
        return await GetApiResponseAsync<AppDateToReturnDto>(
             GetAppDateRequest,
            "Failed to retrieve AppDate from the API.");
    }

    public async Task<bool> CloseDay()
    {
        var response = await ApiRequestHelpers.SendApiRequest(CloseDayRequest);
        return response != null && response.IsSuccessStatusCode;
    }

    private Task<HttpResponseMessage> CloseDayRequest()
        => _httpClient.PutAsync(_apiSettings!.Endpoints!.CloseDay, null);

    private async Task<T> GetApiResponseAsync<T>(
        Func<Task<HttpResponseMessage>> apiRequest,
        string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest);
        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("API call failed: {ErrorMessage}", message ?? "No message provided.");
            return Activator.CreateInstance<T>();
        }

        var content = await response.Content.ReadAsStringAsync();
        var items = ApiRequestHelpers.DeserializeResponseContent<T>(content);

        return items!;
    }

    private Task<HttpResponseMessage> GetAppDateRequest()
        => _httpClient.GetAsync($"{_apiSettings!.Endpoints!.GetAppDate}");

    private async Task<HttpResponseMessage> UpdateOrderCountRequest()
     => await _httpClient.PutAsync(_apiSettings!.Endpoints!.UpdateOrderNumber, null);
}
public class MessageResponseDto
{
    public string? Message { get; set; }
}