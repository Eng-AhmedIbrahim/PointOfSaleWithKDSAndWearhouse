namespace POS.Services.UserSettingsService;
public class UserSetting : IUserSetting
{
    private readonly IUnitOfWork _unitOfWork;

    public UserSetting(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSettingEntity?> AddUserSetting(UserSettingEntity userSetting)
    {
        try
        {
            await _unitOfWork.Repository<UserSettingEntity>().AddAsync(userSetting);
            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
            {
                Log.Error("User Setting Not Added To DataBase AddUserSetting Services");
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"User Setting Not Added To DataBase AddUserSetting Services {ex}");
            return null;
        }

        return userSetting;
    }
    public async Task<UserSettingEntity?> GetUserSetting(string UserId)
    {
        UserSettingEntity userSetting;
        try
        {
            userSetting = await _unitOfWork.Repository<UserSettingEntity>().GetUserSettingByIdAsync(UserId) ?? new();
            if (string.IsNullOrEmpty(userSetting.UserId))
            {
                Log.Error("User Setting Not Found ");
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"User Setting Not Added To DataBase AddUserSetting Services {ex}");
            return null;
        }
        return userSetting;
    }

    public async Task<UserSettingEntity?> UpdateUserSetting(UserSettingEntity userSetting)
    {
        UserSettingEntity oldUserSetting = new();
        try
        {
            oldUserSetting = await _unitOfWork.Repository<UserSettingEntity>().GetUserSettingByIdAsync(userSetting!.UserId!)??new();

            if (oldUserSetting is null)
            {
                var userCreatedSetting = await AddUserSetting(userSetting);
                if (userCreatedSetting is null)
                {
                    return null;
                }
                return userCreatedSetting;
            }
            oldUserSetting.DefaultLanguage = userSetting.DefaultLanguage;
            oldUserSetting.UserName = userSetting.UserName;
            oldUserSetting.CategoryPadding = userSetting.CategoryPadding;
            oldUserSetting.CategoryFontSize = userSetting.CategoryFontSize;

            try
            {
                _unitOfWork.Repository<UserSettingEntity>().Update(oldUserSetting);
                return userSetting;
            }
            catch (Exception ex)
            {
                Log.Error($"Error At UpdateUserSetting Services{ex}");
                return oldUserSetting;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error At UpdateUserSetting Services{ex}");
            return oldUserSetting;

        }


    }
}