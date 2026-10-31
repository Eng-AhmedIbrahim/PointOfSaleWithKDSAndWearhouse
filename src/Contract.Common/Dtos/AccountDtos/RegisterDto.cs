namespace POS.Contract.Dtos.AccountDtos;

public class RegisterDto
{
    [Required]
    public string? UserName { get; set; }

    [Required]
    public string? Password { get; set; }
    public string? DisplayName { get; set; }
    public string? roleName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? ArabicName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public IFormFile? ImageUrl { get; set; }
}