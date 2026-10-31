using POS.Contract;

namespace BlazorBase.ERPFrontServices.DeliveryServices;

public class DeliveryServices : IDeliveryServices
{

    private readonly ApiSettings _apiSettings;
    private readonly ILogger<DeliveryServices> _logger;
    private readonly HttpClient _httpClient;

    public DeliveryServices(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<DeliveryServices> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<IReadOnlyList<DeliveryTitleToReturnDto>> GetAllDeliveryCustomerTitlesAsync()
    {
        return await GetApiResponseAsync<List<DeliveryTitleToReturnDto>>(
          GetAllDeliveryCustomerTitlesRequest,
          "Failed to retrieve Title from the API."
      )??new();
    }

    public async Task<IReadOnlyList<DeliveryZonesToReturnDto>> GetAllDeliveryZonesAsync()
    {
        return await GetApiResponseAsync<List<DeliveryZonesToReturnDto>>(
        GetAllDeliveryZonesRequest,
        "Failed to retrieve Zones from the API."
    ) ?? new List<DeliveryZonesToReturnDto>();
    }

    public async Task<IReadOnlyList<DeliveryZonesToReturnDto>> GetDeliveryZoneByBranchAsync(int branchId)
    {
        return await GetApiResponseAsync<List<DeliveryZonesToReturnDto>>(
        () => GetDeliveryZoneByBranchRequest(branchId),
        "Failed to retrieve Zones from the API."
    ) ?? new List<DeliveryZonesToReturnDto>();
    }


    public async Task<DeliveryCustomerToReturnDto> GetClientByPhoneNumberAsync(string phoneNumber)
    {
            return await GetApiResponseAsync<DeliveryCustomerToReturnDto>(
            () => GetClientByPhoneNumberRequest(phoneNumber),
            "Failed to retrieve Zones from the API."
        )??new();
    }

    public async Task<IReadOnlyList<DeliveryCustomerToReturnDto>> GetAllCustomersAsync()
    {
        return await GetApiResponseAsync<List<DeliveryCustomerToReturnDto>>(
            GetAllDeliveryCustomersRequest,
            "Failed to retrieve all customers from the API."
        ) ?? new List<DeliveryCustomerToReturnDto>();
    }

    private async Task<HttpResponseMessage> GetAllDeliveryCustomersRequest()
        => await _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllDeliveryCustomers);


    public async Task<DeliveryCustomerToReturnDto> CreateClientAsync(DeliveryCustomerDto deliveryCustomer)
    {
        return await GetApiResponseAsync<DeliveryCustomerToReturnDto>(
        () => CreateClientAsyncRequest(deliveryCustomer),
        "Failed to Create Client from the API."
        ) ?? new();
    }

    public async Task<CustomerNewAddressDto> AddNewCustomerAddressAsync(CustomerNewAddressDto newDeliveryCustomerAddress)
    {
        return await GetApiResponseAsync<CustomerNewAddressDto>(
         () => AddNewCustomerAddressRequest(newDeliveryCustomerAddress),
         "Failed to Add New Customer Address from the API."
        ) ?? new();
    }

    private async Task<HttpResponseMessage> AddNewCustomerAddressRequest(CustomerNewAddressDto newDeliveryCustomerAddress)
    => await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.AddNewCustomerAddress, newDeliveryCustomerAddress);

    private async Task<HttpResponseMessage> GetClientByPhoneNumberRequest(string phoneNumber)
     => await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetCustomerByPhone}/{phoneNumber}");

    private async Task<HttpResponseMessage> GetDeliveryZoneByBranchRequest(int branchId)
     => await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetZoneByBranchId}/{branchId}");
    private async Task<HttpResponseMessage> GetAllDeliveryCustomerTitlesRequest()
        => await _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllDeliveryCustomerTitles);

    private async Task<HttpResponseMessage> GetAllDeliveryZonesRequest()
        => await _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllZones);

    public async Task<ServiceResponse<DeliveryZonesToReturnDto>> CreateZone(DeliveryZoneDto newZone)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => CreateZoneRequest(newZone));
            if (response is null)
                return ServiceResponseHelpers.Failure<DeliveryZonesToReturnDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Zone created successfully");

            var createdZone = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<DeliveryZonesToReturnDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return createdZone is null
                ? ServiceResponseHelpers.Failure<DeliveryZonesToReturnDto>(responseMessage)
                : ServiceResponseHelpers.Success(createdZone, responseMessage);
        }, "Failed to Create Zone");
    }

    public async Task<ServiceResponse<DeliveryZonesToReturnDto>> UpdateZone(int id, DeliveryZonesToReturnDto updatedZone)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => UpdateZoneRequest(id, updatedZone));
            if (response is null)
                return ServiceResponseHelpers.Failure<DeliveryZonesToReturnDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Zone updated successfully");

            return response.IsSuccessStatusCode
                ? ServiceResponseHelpers.Success(updatedZone, responseMessage)
                : ServiceResponseHelpers.Failure<DeliveryZonesToReturnDto>(responseMessage);
        }, "Failed to Update Zone");
    }

    public async Task<ServiceResponse<bool>> DeleteZone(int zoneId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => DeleteZoneRequest(zoneId));
            if (response is null)
                return ServiceResponseHelpers.Failure<bool>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Zone Deleted successfully");

            return response.IsSuccessStatusCode
                ? ServiceResponseHelpers.Success(true, responseMessage)
                : ServiceResponseHelpers.Failure<bool>(responseMessage);
        }, "Failed to Delete Zone");
    }

    private Task<HttpResponseMessage> CreateZoneRequest(DeliveryZoneDto zone)
        => _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateZone, zone);

    private Task<HttpResponseMessage> UpdateZoneRequest(int id, DeliveryZonesToReturnDto zone)
        => _httpClient.PutAsJsonAsync($"{_apiSettings.Endpoints!.UpdateZone}/{id}", zone);

    private Task<HttpResponseMessage> DeleteZoneRequest(int id)
        => _httpClient.DeleteAsync($"{_apiSettings.Endpoints!.DeleteZone}/{id}");

    private async Task<HttpResponseMessage> CreateClientAsyncRequest(DeliveryCustomerDto deliveryCustomer)
       => await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateNewCustomer,deliveryCustomer);

    private async Task<T?> GetApiResponseAsync<T>(
    Func<Task<HttpResponseMessage>> apiRequest,
    string? message)
    {
        var response = await ApiRequestHelpers.SendApiRequest(apiRequest);
        if (response is null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("API call failed: {ErrorMessage}", message ?? "No message provided.");
            return default;
        }

        var content = await response.Content.ReadAsStringAsync();
        var result = ApiRequestHelpers.DeserializeResponseContent<T>(content);

        return result;
    }
}