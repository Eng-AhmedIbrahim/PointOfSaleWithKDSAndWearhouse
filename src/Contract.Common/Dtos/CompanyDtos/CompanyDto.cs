namespace POS.Contract.Dtos.CompanyDtos;

public class CompanyDto
{
    public int Id { get; set; }
    public string? EnglishName { get; set; }
    public string? ArabicName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? Website { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreationDate { get; set; }
}
