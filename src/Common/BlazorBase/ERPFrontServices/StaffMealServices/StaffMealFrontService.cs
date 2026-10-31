using POS.Contract.Dtos.AccountDtos;
using POS.Core.Services.Contract;

namespace BlazorBase.ERPFrontServices.StaffMealServices
{
    public class StaffMealFrontService : IStaffMealService
    {
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<StaffMealFrontService> _logger;
        private readonly HttpClient _httpClient;

        public StaffMealFrontService(
            IHttpClientFactory httpClientFactory,
            ApiSettings apiSettings,
            ILogger<StaffMealFrontService> _logger)
        {
            this._apiSettings = apiSettings;
            this._logger = _logger;
            this._httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
        }

        public async Task<StaffMealConfigDto?> GetConfigByUserIdAsync(string userId)
        {
            try { return await _httpClient.GetFromJsonAsync<StaffMealConfigDto>($"api/StaffMeal/config/{userId}"); }
            catch { return null; }
        }

        public async Task<StaffMealStatusDto> GetStatusByUserIdAsync(string userId)
        {
            try { return await _httpClient.GetFromJsonAsync<StaffMealStatusDto>($"api/StaffMeal/status/{userId}") ?? new StaffMealStatusDto { IsEligible = false }; }
            catch { return new StaffMealStatusDto { IsEligible = false }; }
        }

        public async Task<bool> RecordUsageAsync(StaffMealUsageDto usage)
        {
            var response = await _httpClient.PostAsJsonAsync("api/StaffMeal/usage", usage);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<StaffMealConfigDto>> GetAllConfigsAsync()
        {
            try { return await _httpClient.GetFromJsonAsync<IEnumerable<StaffMealConfigDto>>("api/StaffMeal/configs") ?? new List<StaffMealConfigDto>(); }
            catch { return new List<StaffMealConfigDto>(); }
        }

        public async Task<bool> UpsertConfigAsync(StaffMealConfigDto config)
        {
            var response = await _httpClient.PostAsJsonAsync("api/StaffMeal/upsert", config);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> BatchUpsertConfigsAsync(IEnumerable<StaffMealConfigDto> configs)
        {
            var response = await _httpClient.PostAsJsonAsync("api/StaffMeal/batch-upsert", configs);
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<StaffMealGroupDto>> GetAllGroupsAsync()
        {
            try { 
                return 
                    await _httpClient.GetFromJsonAsync<IEnumerable<StaffMealGroupDto>>("api/StaffMeal/groups") 
                    ?? new List<StaffMealGroupDto>(); }
            catch { return new List<StaffMealGroupDto>(); }
        }

        public async Task<StaffMealGroupDto?> GetGroupByIdAsync(int groupId)
        {
            try { return await _httpClient.GetFromJsonAsync<StaffMealGroupDto>($"api/StaffMeal/group/{groupId}"); }
            catch { return null; }
        }

        public async Task<bool> UpsertGroupAsync(StaffMealGroupDto group)
        {
            var response = 
                await _httpClient.PostAsJsonAsync("api/StaffMeal/group", group);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteGroupAsync(int groupId)
        {
            var response = 
                await _httpClient.DeleteAsync($"api/StaffMeal/group/{groupId}");
            return response.IsSuccessStatusCode;
        }
    }
}
