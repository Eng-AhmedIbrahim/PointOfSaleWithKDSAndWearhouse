namespace POS.Core.Entities.UserSettings;

public class UserSettingEntity :BaseEntity
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public decimal CategoryPadding { get; set; } = 8;
    public decimal CategoryFontSize { get; set; } = 20;
    public string DefaultLanguage { get; set; } = "en";
}