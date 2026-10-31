namespace POS.Contract.Dtos.AccountDtos;

public class ForgetPasswordDto
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
}
