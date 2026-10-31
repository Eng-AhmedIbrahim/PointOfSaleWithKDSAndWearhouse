namespace POS.Core.Entities.Discount;

public class DiscountCode : BaseEntity
{
    public int BranchID { get; set; }
    public string? PromotionCode { get; set; }
    public int? PromotionPoints { get; set; }
    public DiscountTypes? DiscountType { get; set; }
    public DateTime? GenerateDate { get; set; }
    public int? GenerateByID { get; set; }
    public string? GenerateByName { get; set; }
    public DateTime? RedeemDate { get; set; }
    public int? RedeemByID { get; set; }
    public string? RedeemByName { get; set; }
    public decimal? RedeemAmount { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? HistoryID { get; set; }
    public int? OrderID { get; set; }
    public int? ClientID { get; set; }
    public int OccasionID { get; set; }
    public DiscountCodesOccasion? DiscountCodesOccasion { get; set; }
}