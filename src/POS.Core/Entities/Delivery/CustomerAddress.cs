namespace POS.Core.Entities.Delivery;

public class CustomerAddress : BaseEntity
{
    public string? BranchName { get; set; }
    public string? ZoneName { get; set; }
    public string? HomeNumber { get; set; }
    public string? FloorNumber { get; set; }
    public string? FlatNumber { get; set; }
    public string? ClientAddress { get; set; }
    public string? AddressNote { get; set; }
    
    public Branch? Branch { get; set; }
    public int? BranchId { get; set; }

    public int? DeliveryZoneId { get; set; }
    public DeliveryZone? DeliveryZone { get; set; } 

    public int DeliveryCustomerId { get; set; }
    public DeliveryCustomerInfo? DeliveryCustomer { get; set; }
}