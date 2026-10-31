using POS.Contract.Dtos.OrderDto;

namespace POS.Core.Services.Contract.PosFeatureServices;

public interface IPosFeatureSettingsService
{
    Task<List<PosFeatureSettingToReturnDto>> GetSettingsByComputerNameAsync(string computerName);
    Task<bool> UpdateSettingsAsync(string computerName, List<PosFeatureSettingToReturnDto> settings);
    Task<bool> InitializeSettingsForComputerAsync(string computerName);
    Task<bool> IsFeatureEnabledAsync(string featureName, string? computerName = null);
}
