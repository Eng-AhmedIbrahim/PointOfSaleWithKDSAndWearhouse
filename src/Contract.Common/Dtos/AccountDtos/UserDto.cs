namespace POS.Contract.Dtos.AccountDtos;

    public class UserDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get => UserId; set => UserId = value; }
        public string? UserId { get; set; }
    public string? UserName { get; set; }
    public bool UpdatedUserPosSetting { get; set; } = false;
    public string? DefaultLanguage { get; set; } 
    public string? Token { get; set; }
    public string? ArabicName { get; set; }
    public string? DisplayName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}