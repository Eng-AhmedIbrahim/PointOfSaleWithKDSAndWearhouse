using POS.Contract;
using BlazorBase.Helpers;
using BlazorBase.API;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace BlazorBase.ERPFrontServices.ItemServices;

public class ItemService : IItemService
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<ItemService> _logger;
    private readonly HttpClient _httpClient;

    public ItemService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<ItemService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<ServiceResponse<IReadOnlyList<MenuSalesItemsToReturnDto>>> GetAllItemsAsync()
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.ItemAPIUrl}/GetAllItems"));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<IReadOnlyList<MenuSalesItemsToReturnDto>>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Items loaded successfully");

            var items = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<IReadOnlyList<MenuSalesItemsToReturnDto>>(
                        await response.Content.ReadAsStringAsync()) : [];

            return items == null
                ? ServiceResponseHelpers.Failure<IReadOnlyList<MenuSalesItemsToReturnDto>>(responseMessage)
                : ServiceResponseHelpers.Success(items, responseMessage);

        }, "Failed to Load Items");
    }

    public async Task<ServiceResponse<IReadOnlyList<ItemsClassificationsDto>>> GetAllClassificationsAsync()
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.ItemAPIUrl}/GetClassifications"));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<IReadOnlyList<ItemsClassificationsDto>>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Classifications loaded successfully");

            var classifications = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<IReadOnlyList<ItemsClassificationsDto>>(
                        await response.Content.ReadAsStringAsync()) : [];

            return classifications == null
                ? ServiceResponseHelpers.Failure<IReadOnlyList<ItemsClassificationsDto>>(responseMessage)
                : ServiceResponseHelpers.Success(classifications, responseMessage);

        }, "Failed to Load Classifications");
    }

    public async Task<ServiceResponse<ItemsClassificationsDto>> CreateClassificationAsync(ItemsClassificationsDto newClassification)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync($"{ConstStringsHelper.ItemAPIUrl}/CreateClassification", newClassification));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<ItemsClassificationsDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Classification created successfully");

            var created = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<ItemsClassificationsDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return created == null
                ? ServiceResponseHelpers.Failure<ItemsClassificationsDto>(responseMessage)
                : ServiceResponseHelpers.Success(created, responseMessage);
        }, "Failed to Create Classification");
    }

    public async Task<ServiceResponse<ItemsClassificationsDto>> UpdateClassificationAsync(ItemsClassificationsDto updatedClassification)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PutAsJsonAsync($"{ConstStringsHelper.ItemAPIUrl}/UpdateClassification", updatedClassification));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<ItemsClassificationsDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Classification updated successfully");

            var updated = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<ItemsClassificationsDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return updated == null
                ? ServiceResponseHelpers.Failure<ItemsClassificationsDto>(responseMessage)
                : ServiceResponseHelpers.Success(updated, responseMessage);
        }, "Failed to Update Classification");
    }

    public async Task<ServiceResponse<bool>> DeleteClassificationAsync(int classificationId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.DeleteAsync($"{ConstStringsHelper.ItemAPIUrl}/DeleteClassification/{classificationId}"));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<bool>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Classification deleted successfully");

            return response.IsSuccessStatusCode
                ? ServiceResponseHelpers.Success(true, responseMessage)
                : ServiceResponseHelpers.Failure<bool>(responseMessage);
        }, "Failed to Delete Classification");
    }

    public async Task<ServiceResponse<MenuSalesItemsToReturnDto>> GetItemByIdAsync(int itemId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.ItemAPIUrl}/{itemId}"));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<MenuSalesItemsToReturnDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, $"Item '{itemId}' loaded successfully");

            var item = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<MenuSalesItemsToReturnDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return item == null
                ? ServiceResponseHelpers.Failure<MenuSalesItemsToReturnDto>(responseMessage)
                : ServiceResponseHelpers.Success(item, responseMessage);

        }, "Failed to Load Item");
    }

    public async Task<ServiceResponse<MenuSalesItemsToReturnDto>> CreateItemAsync(MenuSalesItemsDto newItem)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            // Note: The Item controller expects query parameters or a complex form depending on how itemDto is bound in the backend. 
            // In ItemController: [HttpPost] public async Task<IActionResult> CreateMenuItemAsync([FromQuery] MenuSalesItemsDto itemDto)
            // But usually this responds to POST with a generated query string or JSON. If it's [FromQuery], we have to pass it in query.
            // Let's send as JSON first and see if backend handles it, or post as query.
            
            // To match CategoryService architecture:
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync($"{ConstStringsHelper.ItemAPIUrl}", newItem));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<MenuSalesItemsToReturnDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Item created successfully");

            var createdItem = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<MenuSalesItemsToReturnDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return createdItem == null
                ? ServiceResponseHelpers.Failure<MenuSalesItemsToReturnDto>(responseMessage)
                : ServiceResponseHelpers.Success(createdItem, responseMessage);
        }, "Failed to Create Item");
    }

    public async Task<ServiceResponse<MenuSalesItemsToReturnDto>> UpdateItemAsync(UpdatedItemDto updatedItem)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PutAsJsonAsync(ConstStringsHelper.ItemAPIUrl, updatedItem));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<MenuSalesItemsToReturnDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Item updated successfully");

            var returnedItem = response.IsSuccessStatusCode ?
                   ApiRequestHelpers.DeserializeResponseContent<MenuSalesItemsToReturnDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return returnedItem == null
                ? ServiceResponseHelpers.Failure<MenuSalesItemsToReturnDto>(responseMessage)
                : ServiceResponseHelpers.Success(returnedItem, responseMessage);
        }, "Failed to Update Item");
    }

    public async Task<ServiceResponse<bool>> DeleteItemAsync(int itemId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.DeleteAsync($"{ConstStringsHelper.ItemAPIUrl}/{itemId}"));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<bool>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Item deleted successfully");

            return response.IsSuccessStatusCode
                ? ServiceResponseHelpers.Success(true, responseMessage)
                : ServiceResponseHelpers.Failure<bool>(responseMessage);
        }, "Failed to Delete Item");
    }

    public async Task<ServiceResponse<MenuSalesItemsToReturnDto>> AddAttributeToItemAsync(int attributeId, int itemId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsync($"{ConstStringsHelper.ItemAPIUrl}/AddAttributeToItem?attributeId={attributeId}&ItemId={itemId}", null));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<MenuSalesItemsToReturnDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Attribute linked to item successfully");

            var item = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<MenuSalesItemsToReturnDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return item == null
                ? ServiceResponseHelpers.Failure<MenuSalesItemsToReturnDto>(responseMessage)
                : ServiceResponseHelpers.Success(item, responseMessage);
        }, "Failed to Link Attribute");
    }

    public async Task<ServiceResponse<IReadOnlyList<ItemTypeDto>>> GetItemTypesAsync()
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.ItemAPIUrl}/GetItemTypes"));
            
            if (response is null)
                return ServiceResponseHelpers.Failure<IReadOnlyList<ItemTypeDto>>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Item types loaded successfully");

            var types = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<IReadOnlyList<ItemTypeDto>>(
                        await response.Content.ReadAsStringAsync()) : [];

            return types == null
                ? ServiceResponseHelpers.Failure<IReadOnlyList<ItemTypeDto>>(responseMessage)
                : ServiceResponseHelpers.Success(types, responseMessage);

        }, "Failed to Load Item Types");
    }
}
