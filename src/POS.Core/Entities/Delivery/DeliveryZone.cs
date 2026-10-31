namespace POS.Core.Entities.Delivery;

public class DeliveryZone : BaseEntity
{
    public string? ZoneName { get; set; }
    public decimal? DeliveryFee { get; set; }
    public decimal? ZoneBonus { get; set; }
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public ICollection<CustomerAddress>? CustomerAddresses { get; set; }
}