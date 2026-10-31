namespace POS.Core.Entities.Identity;

public class AppUser : IdentityUser
{
    public DateTime RegistrationDate { get; set; }
    public bool UpdatedUserPosSetting { get; set; } = false;
    public string DefaultLanguage { get; set; } = "en";
    public string? ImageUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? DisplayName { get; set; }
    public string? ArabicName { get; set; }
    public bool IsActive { get; set; } = true;
}