namespace POS.Core.Entities.Customer;

public class CustomerAddressInfo:BaseEntity
{
    public string? HomeNum { get; set; }
    public string? StreetName { get; set; }
    public string? FloorNum { get; set; }
    public string? ApartmentNum { get; set; }
    public string? AddressNote { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
}