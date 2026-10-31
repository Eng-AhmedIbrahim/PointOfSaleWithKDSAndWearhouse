using POS.Contract.Dtos.OrderDto;
using System.Net.Http.Json;
using BlazorBase.API;
using POS.Core.Services.Contract.PosFeatureServices;
using System.Net.Http;

namespace Warehouse.Desktop.Services;

public class DesktopPosFeatureSettingsService : IPosFeatureSettingsService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;

    public DesktopPosFeatureSettingsService(IHttpClientFactory httpClientFactory, ApiSettings apiSettings)
    {
        _apiSettings = apiSettings;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<List<PosFeatureSettingToReturnDto>> GetSettingsByComputerNameAsync(string computerName)
    {
        try
        {
            var settings = await _httpClient.GetFromJsonAsync<List<PosFeatureSettingToReturnDto>>($"{_apiSettings.Endpoints!.GetPosFeatureSettings}/{computerName}");
            return settings ?? new List<PosFeatureSettingToReturnDto>();
        }
        catch (Exception)
        {
            return new List<PosFeatureSettingToReturnDto>();
        }
    }

    public async Task<bool> UpdateSettingsAsync(string computerName, List<PosFeatureSettingToReturnDto> settings)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_apiSettings.Endpoints!.UpdatePosFeatureSettings}/{computerName}", settings);
        return response.IsSuccessStatusCode;
    }

    public Task<bool> InitializeSettingsForComputerAsync(string computerName)
    {
        // This is handled by the server when GetSettingsByComputerNameAsync is called
        return Task.FromResult(true);
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureName, string? computerName = null)
    {
        var settings = await GetSettingsByComputerNameAsync(computerName ?? Environment.MachineName);
        return settings.FirstOrDefault(s => s.FeatureName == featureName)?.Value ?? false;
    }
}

