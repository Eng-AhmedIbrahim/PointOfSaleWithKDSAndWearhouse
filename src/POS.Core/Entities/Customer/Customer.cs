namespace POS.Core.Entities.Customer;

public class Customer : BaseEntity
{
    public int? TitleID { get; set; }
    public CustomerTitle? CustomerTitle { get; set; }
    public string? CustomerName { get; set; }
    public string? PhoneNum1 { get; set; }
    public string? PhoneNum2 { get; set; }
    public int? ZoneID { get; set; }
    public DeliveryZone? DeliveryZone { get; set; }
    public string? LastTakerName { get; set; }
    public int? NumberOfOrders { get; set; }
    public decimal? TotalOfAmounts { get; set; }
    public decimal? DiscountValue { get; set; }
    public string? CustomerNote { get; set; }
    public DateTime? CreationDate { get; set; }
    public ICollection<CustomerAddressInfo>? CustomerAddresses { get; set; }
    public DateTime? FirstOrderDate { get; set; }
    public DateTime? LastOrderDate { get; set; }
}