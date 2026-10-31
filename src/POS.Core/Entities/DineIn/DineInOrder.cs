namespace POS.Core.Entities.DineIn;

public class DineInOrder : BaseEntity
{
    public int OrderId { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? ShiftId { get; set; }
    public string? CashierId { get; set; }
    public string? CashierName { get; set; }
    public string? CaptainId { get; set; }
    public string? CaptainName { get; set; }
    public int TableId { get; set; }
    public string? TableName { get; set; }
    public DateTime OrderDateTime { get; set; }
    public string? OrderState { get; set; } // Open, Closed, Voided
    public decimal? Subtotal { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Service { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountType { get; set; }
    public string? DiscountReason { get; set; }
    public decimal? TotalDiscount { get; set; }
    public decimal? GrandTotal { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? Paid { get; set; }
    public decimal? Remain { get; set; }
    public string? OrderNotice { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime? ClosedDateTime { get; set; }
    public int PrintCount { get; set; }
    
    // Navigation property for order items
    public ICollection<OrderItemsDetails>? OrderDetails { get; set; } = new List<OrderItemsDetails>();
}
