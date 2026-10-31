namespace BlazorBase.ERPFrontServices.CategoryServices;

public class CategoryService : ICategoryServices
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<CategoryService> _logger;
    private readonly HttpClient _httpClient;

    public CategoryService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<CategoryService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<ICollection<CategoryToReturnDto>> GetAllCategoriesAsync()
    {
        return await GetApiResponseAsync<CategoryToReturnDto>(
            GetAllCategoriesRequest,
            "Failed to retrieve categories from the API."
        );
    }

    public async Task<ICollection<MenuSalesItemsToReturnDto>> GetItemsByCategoryIdAsync(
        int categoryId)
    {
        return await GetApiResponseAsync<MenuSalesItemsToReturnDto>(
            () => GetItemsByCategoryIdRequest(categoryId),
            "Failed to retrieve items from the API.");
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

    public async Task<ServiceResponse<CategoryToReturnDto>> CreateCategory(CreateCategoryDto newCategory)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => CreateCategoryRequest(newCategory));
            if (response is null)
                return ServiceResponseHelpers.Failure<CategoryToReturnDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Category created successfully");

            var createdCategory = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<CategoryToReturnDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return createdCategory is null
                ? ServiceResponseHelpers.Failure<CategoryToReturnDto>(responseMessage)
                : ServiceResponseHelpers.Success(createdCategory, responseMessage);
        }, "Failed to Create Category");
    }

    private Task<HttpResponseMessage> GetCategoryByIdRequest(int id)
        => _httpClient.GetAsync($"{ConstStringsHelper.CategoryAPIUrl}/{id}");

    private Task<HttpResponseMessage> CreateCategoryRequest(CreateCategoryDto newCategory)
        => _httpClient.PostAsJsonAsync(ConstStringsHelper.CategoryAPIUrl, newCategory);

    private Task<HttpResponseMessage> UpdateCategoryRequest(UpdatedCategoryDto updatedCategory)
        => _httpClient.PutAsJsonAsync(ConstStringsHelper.CategoryAPIUrl, updatedCategory);
    private Task<HttpResponseMessage> DeleteCategoryRequest(int id)
        => _httpClient.DeleteAsync($"{ConstStringsHelper.CategoryAPIUrl}/{id}");

    public async Task<ServiceResponse<bool>> DeleteCategory(int categoryId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => DeleteCategoryRequest(categoryId));
            if (response is null)
                return ServiceResponseHelpers.Failure<bool>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Category Deleted successfully");

            return response.IsSuccessStatusCode
                ? ServiceResponseHelpers.Success(true, responseMessage)
                : ServiceResponseHelpers.Failure<bool>(responseMessage);
        }, "Failed to Delete Category");
    }

    public async Task<ServiceResponse<IReadOnlyList<CategoryToReturnDto>>> GetAllCategories()
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(GetAllCategoriesRequest);
            if (response is null)
                return ServiceResponseHelpers.Failure<IReadOnlyList<CategoryToReturnDto>>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Categories loaded successfully");

            var ctegories = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<IReadOnlyList<CategoryToReturnDto>>(
                        await response.Content.ReadAsStringAsync()) : [];

            return ctegories is null
                ? ServiceResponseHelpers.Failure<IReadOnlyList<CategoryToReturnDto>>(responseMessage)
                : ServiceResponseHelpers.Success(ctegories, responseMessage);

        }
        , "Failed to Load Categories");
    }

    public async Task<ServiceResponse<CategoryToReturnDto>> GetCategoryById(int categoryId)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => GetCategoryByIdRequest(categoryId));
            if (response is null)
                return ServiceResponseHelpers.Failure<CategoryToReturnDto>("Failed to connect to the API");

            var responseMessage =
                await ApiRequestHelpers.GetResponseMessage(response, $"Category with '{categoryId}' loaded successfully");

            var category = response.IsSuccessStatusCode ?
                    ApiRequestHelpers.DeserializeResponseContent<CategoryToReturnDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return category is null
                ? ServiceResponseHelpers.Failure<CategoryToReturnDto>(responseMessage)
                : ServiceResponseHelpers.Success(category, responseMessage);

        }, "Failed to Load Category");
    }

    public async Task<ServiceResponse<CategoryToReturnDto>> UpdateCategory(UpdatedCategoryDto updatedCategory)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            var response = await ApiRequestHelpers.SendApiRequest(() => UpdateCategoryRequest(updatedCategory));
            if (response is null)
                return ServiceResponseHelpers.Failure<CategoryToReturnDto>("Failed to connect to the API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Category created successfully");

            var returnedCategory = response.IsSuccessStatusCode ?
                   ApiRequestHelpers.DeserializeResponseContent<CategoryToReturnDto>(
                        await response.Content.ReadAsStringAsync()) : default;

            return returnedCategory is null
                ? ServiceResponseHelpers.Failure<CategoryToReturnDto>(responseMessage)
                : ServiceResponseHelpers.Success(returnedCategory, responseMessage);
        }, "Failed to Update Category");
    }

    private Task<HttpResponseMessage> GetAllCategoriesRequest()
        => _httpClient.GetAsync(_apiSettings.Endpoints!.GetAllCategories);

    private Task<HttpResponseMessage> GetItemsByCategoryIdRequest(int categoryId)
        => _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetItemsByCategoryId}?catId={categoryId}");
}