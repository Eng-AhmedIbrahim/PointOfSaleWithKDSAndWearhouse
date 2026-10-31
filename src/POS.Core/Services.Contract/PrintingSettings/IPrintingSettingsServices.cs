using POS.Core.Entities.Kitchen;

namespace POS.Core.Services.Contract.PrintingSettings;

public interface IPrintingSettingsServices
{
    Task<List<POS.Core.Entities.Kitchen.PrintingSettings>> GetSettingsByComputerNameAsync(string computerName);
    Task<bool> UpdateSettingsAsync(List<POS.Core.Entities.Kitchen.PrintingSettings> settings);
    Task<bool> InitializeSettingsForComputerAsync(string computerName);
}
