namespace POS.Contract.Dtos.AccountDtos;

public class LoginDto
{
    [Required]
    public string? UserName { get; set; }

    [Required]
    public string? Password { get; set; }

    public bool? ForBackOffice { get; set; }
}