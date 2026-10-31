namespace POS.Contract.Dtos.AccountDtos;

public class AccountUserToReturnDto
{
    public string? Id { get; set; }
    public string? UserName { get; set; }
    public string? DisplayName { get; set; }
    public string? ArabicName { get; set; }
    public bool UpdatedUserPosSetting { get; set; } = false;
    public string? DefaultLanguage { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public RoleToReturnDto Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
