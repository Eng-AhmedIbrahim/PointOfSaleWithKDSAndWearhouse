using POS.Contract.Dtos.SettingsDtos;
using POS.Contract.Dtos.Common;

namespace POS.Core.Services.Contract.DataSyncServices;

public interface IDataSyncService
{
    Task<HqSettingDto> GetHqSettingsAsync();
    Task<BaseResponse> UpdateHqSettingsAsync(HqSettingDto settings);
    Task<BaseResponse> SyncDataFromHqAsync(SyncRequestDto request);
    Task<BaseResponse> TestHqConnectionAsync(HqSettingDto settings);
    Task<List<string>> GetDatabasesAsync(HqSettingDto settings);
}
