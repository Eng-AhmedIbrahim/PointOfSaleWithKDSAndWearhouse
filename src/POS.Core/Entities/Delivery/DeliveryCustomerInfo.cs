namespace POS.Core.Entities.Delivery;

public class DeliveryCustomerInfo : BaseEntity
{
    public string? FirstPhoneNumber { get; set; }
    public string? SecondPhoneNumber { get; set; }
    public string? ClientTitle { get; set; }
    public string? CustomerName { get; set; }

    public ICollection<CustomerAddress>? CustomerAddresses { get; set; }
}