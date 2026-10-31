namespace POS.Core.Entities.Discount;

public class DiscountReason : BaseEntity
{
    public int BranchID { get; set; } 
    public string? EnglishReason { get; set; } 
    public string? ArabicReason { get; set; } 
    public DateTime? CreationDate { get; set; } 
}