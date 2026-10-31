namespace BlazorBase.Models;

public class OrderDiscount
{
    public string? DiscountType { get; set; }
    public decimal Percentage { get; set; } = 0M;
    public decimal Value { get; set; } = 0M;
    public string? DiscountReason { get; set; }
}
