namespace POS.API.Controllers;
public class UserSettingsController : BaseApiController
{
    private readonly IUserSetting _userSetting;

    public UserSettingsController(IUserSetting userSetting)
    {
        _userSetting = userSetting;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUserSettings(string userId)
    {
        var userSetting = await _userSetting.GetUserSetting(userId);
        if (userSetting is null)
            return NotFound(new ApiResponse(404));
        return Ok(userSetting);
    }
    [HttpPost]
    public async Task<IActionResult> CreateUserSetting(UserSettingEntity userSettings)
    {
       var userSetting= await _userSetting.UpdateUserSetting(userSettings);
        if (userSetting is null)
            return NotFound(new ApiResponse(404));
        return Ok(userSetting);
    }
}