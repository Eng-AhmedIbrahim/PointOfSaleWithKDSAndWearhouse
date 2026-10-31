namespace POS.Core.Entities.Company;

public class Company : BaseEntity
{
    public string? EnglishName { get; set; }
    public string? NormalizedEnglishName { get; set; }
    public string? ArabicName { get; set; }
    public string? PhoneNumber { get; set; } 
    public string? MobileNumber { get; set; } 
    public string? Website { get; set; } 
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? Address { get; set; }
    public DateTime CreationDate { get; set; }

    public ICollection<Branch>? Branches { get; set; } = [];
}