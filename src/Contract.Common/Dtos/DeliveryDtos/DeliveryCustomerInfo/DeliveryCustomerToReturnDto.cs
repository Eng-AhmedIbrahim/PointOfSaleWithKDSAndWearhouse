namespace POS.Contract.Dtos.DeliveryDtos.DeliveryCustomerInfo;

public class DeliveryCustomerToReturnDto
{
    public int Id { get; set; }
    public string? FirstPhoneNumber { get; set; }
    public string? SecondPhoneNumber { get; set; }
    public string? ClientTitle { get; set; }
    public string? CustomerName { get; set; }

    public ICollection<CustomerAddressDto>? CustomerAddresses { get; set; }

    // Statistics
    public DateTime? FirstOrderDate { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public string? LastReceiverName { get; set; }
    public decimal TotalOrdersAmount { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }

    // History & Active Order
    public List<POS.Contract.Dtos.OrderDtos.OrderDto>? Last10Orders { get; set; }
    public List<POS.Contract.Dtos.OrderDtos.OrderDto>? ActiveOrders { get; set; }
}