namespace POS.Core.Services.Contract.UserSettingServices;

public interface IUserSetting
{
    public Task<UserSettingEntity?> AddUserSetting(UserSettingEntity userSetting);
    public Task<UserSettingEntity?> UpdateUserSetting(UserSettingEntity userSetting);
    public Task<UserSettingEntity?> GetUserSetting(string UserId);
}