/* file: Common/BlazorBase/ERPFrontServices/SettingsServices/ISystemSettingsServices.cs */
using POS.Contract;
using POS.Contract.Dtos.SettingsDtos;
using System.Threading.Tasks;

namespace BlazorBase.ERPFrontServices.SettingsServices;

public interface ISystemSettingsServices
{
    Task<DispatcherSettingsDto> GetDispatcherSettingsAsync();
    Task<ServiceResponse<DispatcherSettingsDto>> UpdateDispatcherSettingsAsync(DispatcherSettingsDto settings);
}
