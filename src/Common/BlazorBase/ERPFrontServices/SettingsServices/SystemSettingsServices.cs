/* file: Common/BlazorBase/ERPFrontServices/SettingsServices/SystemSettingsServices.cs */
using BlazorBase.API;
using BlazorBase.Helpers;
using Microsoft.Extensions.Logging;
using POS.Contract;
using POS.Contract.Dtos.SettingsDtos;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using POS.Core.Services.Contract;
using ERPFront.Models;


namespace BlazorBase.ERPFrontServices.SettingsServices;

public class SystemSettingsServices : ISystemSettingsServices
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<SystemSettingsServices> _logger;
    private readonly DispatcherSettings? _localDispatcherSettings;


    public SystemSettingsServices(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        ILogger<SystemSettingsServices> logger,
        IOptions<DispatcherSettings>? localDispatcherSettings = null)
    {
        _apiSettings = apiSettings;
        _logger = logger;
        _localDispatcherSettings = localDispatcherSettings?.Value;
        _httpClient = httpClientFactory.CreateClient(_apiSettings!.ApiName!);
    }


    public async Task<DispatcherSettingsDto> GetDispatcherSettingsAsync()
    {
        // Try local settings first (from appsettings.json in Desktop)
        if (_localDispatcherSettings != null)
        {
            return new DispatcherSettingsDto
            {
                ComputerName = Environment.MachineName,
                RefreshTimeForDeliveryOrderColorsPerSecond = _localDispatcherSettings.RefreshTimeForDeliveryOrderColorsPerSecond,
                CriticalTimeForDeliveryOrderPerMinute = _localDispatcherSettings.CriticalTimeForDeliveryOrderPerMinute,
                WarningTimeForDeliveryOrderPerMinute = _localDispatcherSettings.WarningTimeForDeliveryOrderPerMinute,
                VoidLimitMinutesForDeliveryOrder = _localDispatcherSettings.VoidLimitMinutesForDeliveryOrder,
                IsDispatcher = _localDispatcherSettings.IsDispatcher,
                AllowVoidLimitMinutesForDeliveryOrder = _localDispatcherSettings.AllowVoidLimitMinutesForDeliveryOrder,
                AllowDeliveryVoidFromBranch = _localDispatcherSettings.AllowDeliveryVoidFromBranch
            };
        }

        try
        {
            string computerName = Environment.MachineName;
            var response = await ApiRequestHelpers.SendApiRequest(() => 
                _httpClient.GetAsync($"{_apiSettings.Endpoints!.GetDispatcherSettings}/{computerName}"));
            
            if (response != null && response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return ApiRequestHelpers.DeserializeResponseContent<DispatcherSettingsDto>(content) ?? new DispatcherSettingsDto { ComputerName = computerName };
            }
            return new DispatcherSettingsDto { ComputerName = computerName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dispatcher settings");
            return new DispatcherSettingsDto { ComputerName = Environment.MachineName };
        }
    }

    public async Task<ServiceResponse<DispatcherSettingsDto>> UpdateDispatcherSettingsAsync(DispatcherSettingsDto settings)
    {
        return await ServiceResponseHelpers.ExecuteWithResponseAsync(async () =>
        {
            if (string.IsNullOrEmpty(settings.ComputerName)) settings.ComputerName = Environment.MachineName;
            
            var response = await ApiRequestHelpers.SendApiRequest(() => 
                _httpClient.PostAsJsonAsync(_apiSettings.Endpoints!.UpdateDispatcherSettings, settings));
            
            if (response == null) return ServiceResponseHelpers.Failure<DispatcherSettingsDto>("Failed to connect to API");

            var responseMessage = await ApiRequestHelpers.GetResponseMessage(response, "Settings updated successfully");
            var result = response.IsSuccessStatusCode 
                ? ApiRequestHelpers.DeserializeResponseContent<DispatcherSettingsDto>(await response.Content.ReadAsStringAsync()) 
                : default;

            return result == null 
                ? ServiceResponseHelpers.Failure<DispatcherSettingsDto>(responseMessage) 
                : ServiceResponseHelpers.Success(result, responseMessage);
        }, "Failed to update settings");
    }
}
