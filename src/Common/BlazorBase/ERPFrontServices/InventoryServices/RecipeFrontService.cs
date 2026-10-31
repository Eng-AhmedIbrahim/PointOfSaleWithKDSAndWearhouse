using System.Net.Http.Json;
using POS.Contract.Dtos.InventoryDtos;
using BlazorBase.Helpers;

namespace BlazorBase.ERPFrontServices.InventoryServices;

public class RecipeFrontService : IRecipeFrontService
{
    private readonly HttpClient _httpClient;

    public RecipeFrontService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ServiceResponse<IReadOnlyList<RecipeDto>>> GetAllRecipesAsync()
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.RecipeAPIUrl}/GetAll"));
            return await HandleResponse<IReadOnlyList<RecipeDto>>(response, "Recipes loaded");
        }, "Failed to Load Recipes");
    }

    public async Task<ServiceResponse<RecipeDto>> GetRecipeByItemIdAsync(int itemId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.GetAsync($"{ConstStringsHelper.RecipeAPIUrl}/{itemId}"));
            return await HandleResponse<RecipeDto>(response, "Recipe loaded");
        }, "Failed to Load Recipe");
    }

    public async Task<ServiceResponse<bool>> CreateRecipeAsync(RecipeDto recipe)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PostAsJsonAsync(ConstStringsHelper.RecipeAPIUrl, recipe));
            return await HandleResponse<bool>(response, "Recipe created");
        }, "Failed to Create Recipe");
    }

    public async Task<ServiceResponse<bool>> UpdateRecipeAsync(RecipeDto recipe)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.PutAsJsonAsync(ConstStringsHelper.RecipeAPIUrl, recipe));
            return await HandleResponse<bool>(response, "Recipe updated");
        }, "Failed to Update Recipe");
    }

    public async Task<ServiceResponse<bool>> DeleteRecipeAsync(int id)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => _httpClient.DeleteAsync($"{ConstStringsHelper.RecipeAPIUrl}/{id}"));
            return await HandleResponse<bool>(response, "Recipe deleted");
        }, "Failed to Delete Recipe");
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
