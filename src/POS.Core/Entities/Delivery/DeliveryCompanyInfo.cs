namespace POS.Core.Entities.Delivery;

public class DeliveryCompanyInfo:BaseEntity
{
    public string? DeliveryCompanyName { get; set; }
    public string? Address { get; set; }
    public ICollection<Orders>? Orders  { get; set; }
    public DateTime? AssignTime { get; set; }
    public DateTime? BackTime { get; set; }
    public int? TotalOrdersAmount { get; set; }
    public decimal? TotalOrdersCash { get; set; }
}