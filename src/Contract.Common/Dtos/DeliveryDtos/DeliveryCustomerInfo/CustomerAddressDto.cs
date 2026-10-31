namespace POS.Contract.Dtos.DeliveryDtos.DeliveryCustomerInfo;

public class CustomerAddressDto
{
    public string? BranchName { get; set; }
    public string? ZoneName { get; set; }
    public string? HomeNumber { get; set; }
    public string? FloorNumber { get; set; }
    public string? FlatNumber { get; set; }
    public string? ClientAddress { get; set; }
    public string? AddressNote { get; set; }
    public int Id { get; set; }
    public int DeliveryZoneId { get; set; }
    public int DeliveryCustomerId { get; set; }
    public int BranchId { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal ZoneBonus { get; set; }

}