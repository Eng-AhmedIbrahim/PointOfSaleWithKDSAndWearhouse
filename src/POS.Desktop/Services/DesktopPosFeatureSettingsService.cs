using POS.Contract.Dtos.OrderDto;
using System.Net.Http.Json;
using BlazorBase.API;
using POS.Core.Services.Contract.PosFeatureServices;
using System.Net.Http;

namespace POS.Desktop.Services;

public class DesktopPosFeatureSettingsService : IPosFeatureSettingsService
{
    private readonly HttpClient _httpClient;

    public DesktopPosFeatureSettingsService(IHttpClientFactory httpClientFactory, ApiSettings apiSettings)
    {
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<List<PosFeatureSettingToReturnDto>> GetSettingsByComputerNameAsync(string computerName)
    {
        try
        {
            var settings = await _httpClient.GetFromJsonAsync<List<PosFeatureSettingToReturnDto>>($"api/PosFeatureSettings/{computerName}");
            return settings ?? new List<PosFeatureSettingToReturnDto>();
        }
        catch (Exception)
        {
            return new List<PosFeatureSettingToReturnDto>();
        }
    }

    public async Task<bool> UpdateSettingsAsync(string computerName, List<PosFeatureSettingToReturnDto> settings)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/PosFeatureSettings/{computerName}", settings);
        return response.IsSuccessStatusCode;
    }

    public Task<bool> InitializeSettingsForComputerAsync(string computerName)
    {
        // This is handled by the server when GetSettingsByComputerNameAsync is called
        return Task.FromResult(true);
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureName, string? computerName = null)
    {
        if (string.IsNullOrEmpty(computerName))
            computerName = Environment.MachineName;

        var settings = await GetSettingsByComputerNameAsync(computerName);
        var setting = settings.FirstOrDefault(x => x.FeatureName == featureName);
        return setting?.Value ?? false;
    }
}
