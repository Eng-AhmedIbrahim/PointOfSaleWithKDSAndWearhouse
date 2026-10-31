using System.Net.Http.Json;
using BlazorBase.API;
using POS.Contract.Dtos.Common;
using POS.Contract.Dtos.SettingsDtos;

namespace BlazorBase.ERPFrontServices.DataSyncServices;

public class DataSyncFrontService : IDataSyncFrontService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;

    public DataSyncFrontService(IHttpClientFactory httpClientFactory, ApiSettings apiSettings)
    {
        _apiSettings = apiSettings;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    public async Task<HqSettingDto> GetHqSettingsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<HqSettingDto>(_apiSettings.Endpoints!.GetHqSettings!) ?? new HqSettingDto();
        }
        catch
        {
            return new HqSettingDto();
        }
    }

    public async Task<BaseResponse> UpdateHqSettingsAsync(HqSettingDto settings)
    {
        var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.UpdateHqSettings!, settings);
        return await response.Content.ReadFromJsonAsync<BaseResponse>() ?? new BaseResponse { Success = false, Message = "API Error" };
    }

    public async Task<BaseResponse> TestHqConnectionAsync(HqSettingDto settings)
    {
        var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.TestHqConnection!, settings);
        return await response.Content.ReadFromJsonAsync<BaseResponse>() ?? new BaseResponse { Success = false, Message = "API Error" };
    }

    public async Task<BaseResponse> SyncHqDataAsync(SyncRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.SyncHqData!, request);
        return await response.Content.ReadFromJsonAsync<BaseResponse>() ?? new BaseResponse { Success = false, Message = "API Error" };
    }

    public async Task<List<string>> GetDatabasesAsync(HqSettingDto settings)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/DataSync/databases", settings);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
        }
        return new List<string>();
    }
}
