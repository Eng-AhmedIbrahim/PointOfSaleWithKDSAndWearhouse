using POS.Contract;
using POS.Contract.Dtos.DineInDtos;

namespace BlazorBase.ERPFrontServices.DineInServices;

public class DineInService : IDineInService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<DineInService> _logger;

    public DineInService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<DineInService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(_apiSettings!.ApiName!);
    }
    public async Task<ICollection<TableGroupToReturnDto>> GetTableGroupsAsync()
    {
        return await GetApiResponseAsync<TableGroupToReturnDto>(
              GetAllTableGroupsRequest,
              "Failed to retrieve Table Groups from the API."
          );
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

    private Task<HttpResponseMessage> GetAllTableGroupsRequest()
    => _httpClient.GetAsync(_apiSettings.Endpoints!.GetTableGroups!);

    public async Task<ICollection<TableToReturnDto>> GetTablesByGroupId(int tableGroupId)
    {
        return await GetApiResponseAsync<TableToReturnDto>(
            () => GetTablesByGroupIdRequest(tableGroupId),
             "Failed to retrieve Table Groups from the API."
         );
    }

    public async Task<ICollection<UserToReturnDto>> GetCaptainOrders()
        => await GetApiResponseAsync<UserToReturnDto>(GetCaptainOrdersRequest,
            "Failed to retrieve Captain Orders from the API.");

    private async Task<HttpResponseMessage> GetTablesByGroupIdRequest(int groupId)
    => await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetTablesByGroupId}/{groupId}");

    private async Task<HttpResponseMessage> GetCaptainOrdersRequest()
        => await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetUsersByRole}/5");

    public async Task<ICollection<TableToReturnDto>> GetTables()
    {
        return await GetApiResponseAsync<TableToReturnDto>(
             GetTablesRequest,
             "Failed to retrieve Tables from the API."
         );
    }

    private async Task<HttpResponseMessage> GetTablesRequest()
     =>   await _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetAllTables}");

    // CRUD operations for Table Groups
    public async Task<ServiceResponse<TableGroupToReturnDto>> CreateTableGroup(TableGroupDto tableGroup)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateTableGroup, tableGroup));
            if (response is null) return ServiceResponseHelpers.Failure<TableGroupToReturnDto>("Failed to connect to the API");
            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Table Group created successfully");
            var result = response.IsSuccessStatusCode ? ApiRequestHelpers.DeserializeResponseContent<TableGroupToReturnDto>(await response.Content.ReadAsStringAsync()) : default;
            return result is null ? ServiceResponseHelpers.Failure<TableGroupToReturnDto>(responseMessage) : ServiceResponseHelpers.Success(result, responseMessage);
        }, "Failed to Create Table Group");
    }

    public async Task<ServiceResponse<TableGroupToReturnDto>> UpdateTableGroup(int id, TableGroupToReturnDto tableGroup)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PutAsJsonAsync($"{_apiSettings.Endpoints!.UpdateTableGroup}/{id}", tableGroup));
            if (response is null) return ServiceResponseHelpers.Failure<TableGroupToReturnDto>("Failed to connect to the API");
            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Table Group updated successfully");
            return response.IsSuccessStatusCode ? ServiceResponseHelpers.Success(tableGroup, responseMessage) : ServiceResponseHelpers.Failure<TableGroupToReturnDto>(responseMessage);
        }, "Failed to Update Table Group");
    }

    public async Task<ServiceResponse<bool>> DeleteTableGroup(int id)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.DeleteAsync($"{_apiSettings.Endpoints!.DeleteTableGroup}/{id}"));
            if (response is null) return ServiceResponseHelpers.Failure<bool>("Failed to connect to the API");
            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Table Group Deleted successfully");
            return response.IsSuccessStatusCode ? ServiceResponseHelpers.Success(true, responseMessage) : ServiceResponseHelpers.Failure<bool>(responseMessage);
        }, "Failed to Delete Table Group");
    }

    // CRUD operations for Tables
    public async Task<ServiceResponse<TableToReturnDto>> CreateTable(TableDto table)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.CreateTable, table));
            if (response is null) return ServiceResponseHelpers.Failure<TableToReturnDto>("Failed to connect to the API");
            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Table created successfully");
            var result = response.IsSuccessStatusCode ? ApiRequestHelpers.DeserializeResponseContent<TableToReturnDto>(await response.Content.ReadAsStringAsync()) : default;
            return result is null ? ServiceResponseHelpers.Failure<TableToReturnDto>(responseMessage) : ServiceResponseHelpers.Success(result, responseMessage);
        }, "Failed to Create Table");
    }

    public async Task<ServiceResponse<TableToReturnDto>> UpdateTable(int id, TableToReturnDto table)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PutAsJsonAsync($"{_apiSettings.Endpoints!.UpdateTable}/{id}", table));
            if (response is null) return ServiceResponseHelpers.Failure<TableToReturnDto>("Failed to connect to the API");
            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Table updated successfully");
            return response.IsSuccessStatusCode ? ServiceResponseHelpers.Success(table, responseMessage) : ServiceResponseHelpers.Failure<TableToReturnDto>(responseMessage);
        }, "Failed to Update Table");
    }

    public async Task<ServiceResponse<bool>> DeleteTable(int id)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.DeleteAsync($"{_apiSettings.Endpoints!.DeleteTable}/{id}"));
            if (response is null) return ServiceResponseHelpers.Failure<bool>("Failed to connect to the API");
            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Table Deleted successfully");
            return response.IsSuccessStatusCode ? ServiceResponseHelpers.Success(true, responseMessage) : ServiceResponseHelpers.Failure<bool>(responseMessage);
        }, "Failed to Delete Table");
    }
}