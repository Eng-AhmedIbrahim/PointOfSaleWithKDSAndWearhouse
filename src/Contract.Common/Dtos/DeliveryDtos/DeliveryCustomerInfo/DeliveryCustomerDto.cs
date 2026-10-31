namespace POS.Contract.Dtos.DeliveryDtos.DeliveryCustomerInfo;

public class DeliveryCustomerDto
{
    public string? FirstPhoneNumber { get; set; }
    public string? SecondPhoneNumber { get; set; }
    public string? ClientTitle { get; set; }
    public string? CustomerName { get; set; }   
    public string? BranchName { get; set; }
    public string? ZoneName { get; set; }
    public string? HomeNumber { get; set; }
    public string? FloorNumber { get; set; }
    public string? FlatNumber { get; set; }
    public string? ClientAddress { get; set; }
    public string? AddressNote { get; set; }
    public int? DeliveryZoneId { get; set; }
    public int? BranchId { get; set; }
}
