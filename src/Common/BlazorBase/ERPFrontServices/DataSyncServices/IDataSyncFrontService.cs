using POS.Contract.Dtos.Common;
using POS.Contract.Dtos.SettingsDtos;

namespace BlazorBase.ERPFrontServices.DataSyncServices;

public interface IDataSyncFrontService
{
    Task<HqSettingDto> GetHqSettingsAsync();
    Task<BaseResponse> UpdateHqSettingsAsync(HqSettingDto settings);
    Task<BaseResponse> TestHqConnectionAsync(HqSettingDto settings);
    Task<BaseResponse> SyncHqDataAsync(SyncRequestDto request);
    Task<List<string>> GetDatabasesAsync(HqSettingDto settings);
}
