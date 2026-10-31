namespace POS.Core.Entities.Takeaway;

public class TakeawayOrder:BaseEntity
{
    public int OrderId { get; set; }
    public int BranchID { get; set; }
    public string? BranchName { get; set; }
    public int? ShiftID { get; set; } = 1;
    public string? CashierID { get; set; }
    public string? CashierName { get; set; }
    public string? OrderState { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal? Tax { get; set; }
    public decimal? DiscountOrder { get; set; }
    public decimal? DiscountedItems { get; set; }
    public decimal? TotalDiscount { get; set; }
    public decimal? CashPaid { get; set; }
    public decimal? VisaPaid { get; set; }
    public decimal? InstaPay { get; set; }
    public decimal? Wallet { get; set; }
    public decimal? GrandTotal { get; set; }
    public decimal? Paid { get; set; }
    public decimal? Remain { get; set; }
    public string? DiscountByName { get; set; }
    public string? DiscountReason { get; set; }
    public bool? WithoutTax { get; set; }
    public string? OrderNotice { get; set; }
    public decimal? VoidAmount { get; set; }
    public string? VoidByName { get; set; }
    public DateTime? VoidTime { get; set; }
    public string? VoidReason { get; set; }
    public int? VoidCount { get; set; }
    public decimal? TotalVoid { get; set; }
    public int? PrintCount { get; set; }

    public ICollection<OrderItemsDetails>? OrderDetails { get; set; } = new List<OrderItemsDetails>();
}
