using POS.Contract.Dtos.InventoryDtos;
using BlazorBase.Helpers;
using BlazorBase.API;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using POS.Contract;

namespace BlazorBase.ERPFrontServices.InventoryServices;

public class InventoryFrontService : IInventoryFrontService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryFrontService> _logger;

    public InventoryFrontService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<InventoryFrontService> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<ServiceResponse<IReadOnlyList<InventoryItemDto>>> GetAllInventoryAsync()
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.InventoryAPIUrl}/GetAll"));
            var data = await HandleResponse<IReadOnlyList<InventoryItemDto>>(response, "Inventory loaded");
            return data;
        }, "Failed to Load Inventory");
    }

    public async Task<ServiceResponse<InventoryItemDto>> GetInventoryByItemIdAsync(int itemId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.InventoryAPIUrl}/{itemId}"));
            return await HandleResponse<InventoryItemDto>(response, "Item inventory loaded");
        }, "Failed to Load Item Inventory");
    }

    public async Task<ServiceResponse<bool>> UpdateStockAsync(UpdateStockDto updateDto)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync($"{ConstStringsHelper.InventoryAPIUrl}/UpdateStock", updateDto));
            return await HandleResponse<bool>(response, "Stock updated");
        }, "Failed to Update Stock");
    }

    public async Task<ServiceResponse<bool>> SetOpeningStockAsync(UpdateStockDto updateDto)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync($"{ConstStringsHelper.InventoryAPIUrl}/SetOpeningStock", updateDto));
            return await HandleResponse<bool>(response, "Opening stock set");
        }, "Failed to Set Opening Stock");
    }

    public async Task<ServiceResponse<bool>> SetPhysicalStockAsync(UpdateStockDto updateDto)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync($"{ConstStringsHelper.InventoryAPIUrl}/SetPhysicalStock", updateDto));
            return await HandleResponse<bool>(response, "Physical stock set");
        }, "Failed to Set Physical Stock");
    }

    public async Task<ServiceResponse<bool>> InitializeInventoryAsync(InventoryItemDto initDto)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync($"{ConstStringsHelper.InventoryAPIUrl}/Initialize", initDto));
            return await HandleResponse<bool>(response, "Inventory initialized");
        }, "Failed to Initialize Inventory");
    }

    public async Task<ServiceResponse<bool>> InitializeAllItemsAsync()
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsync($"{ConstStringsHelper.InventoryAPIUrl}/InitializeAll", null));
            return await HandleResponse<bool>(response, "All items initialized");
        }, "Failed to Initialize All Items");
    }

    public async Task<ServiceResponse<IReadOnlyList<InventoryTransactionDto>>> GetTransactionsAsync(int itemId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.InventoryAPIUrl}/Transactions/{itemId}"));
            return await HandleResponse<IReadOnlyList<InventoryTransactionDto>>(response, "Transactions loaded");
        }, "Failed to Load Transactions");
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
