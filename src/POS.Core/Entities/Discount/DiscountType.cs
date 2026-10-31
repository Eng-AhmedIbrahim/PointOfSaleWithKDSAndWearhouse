namespace POS.Core.Entities.Discount;

public class DiscountType : BaseEntity
{
    public int BranchID { get; set; } = 1;
    public float? Value { get; set; }
    public DiscountTypes? DiscountTypes { get; set; }
    public string? TimeStamp { get; set; } 
}