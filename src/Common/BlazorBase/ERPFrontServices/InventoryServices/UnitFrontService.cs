using POS.Contract.Dtos.InventoryDtos;
using BlazorBase.Helpers;
using BlazorBase.API;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace BlazorBase.ERPFrontServices.InventoryServices;

public class UnitFrontService : IUnitFrontService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UnitFrontService> _logger;

    public UnitFrontService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<UnitFrontService> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<ServiceResponse<IEnumerable<UnitDto>>> GetAllUnitsAsync()
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync(ConstStringsHelper.UnitAPIUrl));
            return await HandleResponse<IEnumerable<UnitDto>>(response, "Units loaded");
        }, "Failed to Load Units");
    }

    public async Task<ServiceResponse<UnitDto>> GetUnitByIdAsync(int id)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.UnitAPIUrl}/{id}"));
            return await HandleResponse<UnitDto>(response, "Unit loaded");
        }, "Failed to Load Unit");
    }

    public async Task<ServiceResponse<bool>> CreateUnitAsync(UnitDto unit)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync(ConstStringsHelper.UnitAPIUrl, unit));
            return await HandleResponse<bool>(response, "Unit created");
        }, "Failed to Create Unit");
    }

    public async Task<ServiceResponse<bool>> UpdateUnitAsync(UnitDto unit)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PutAsJsonAsync(ConstStringsHelper.UnitAPIUrl, unit));
            return await HandleResponse<bool>(response, "Unit updated");
        }, "Failed to Update Unit");
    }

    public async Task<ServiceResponse<bool>> DeleteUnitAsync(int id)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.DeleteAsync($"{ConstStringsHelper.UnitAPIUrl}/{id}"));
            return await HandleResponse<bool>(response, "Unit deleted");
        }, "Failed to Delete Unit");
    }

    private async Task<ServiceResponse<T>> HandleResponse<T>(HttpResponseMessage? response, string successMessage)
    {
        if (response is null)
            return ServiceResponseHelpers.Failure<T>("Failed to connect to API");

        var message = await ApiRequestHelpers.GetResponseMessage(response, successMessage);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var data = ApiRequestHelpers.DeserializeResponseContent<T>(content);
            return ServiceResponseHelpers.Success(data!, message);
        }
        return ServiceResponseHelpers.Failure<T>(message);
    }
}
