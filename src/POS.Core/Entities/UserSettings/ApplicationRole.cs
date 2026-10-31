namespace POS.Core.Entities.UserSettings;

public class ApplicationRole : IdentityRole
{
    public bool ShowInLogin { get; set; } = true;
}

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PoliceArabicName { get; set; } = string.Empty;
    public string PoliceEnglishNameEn { get; set; } = string.Empty;
    public string ScreenArabicName { get; set; } = string.Empty;
    public string ScreenEnglishName { get; set; } = string.Empty;
    public bool IsBackOffice { get; set; } = false;
}
