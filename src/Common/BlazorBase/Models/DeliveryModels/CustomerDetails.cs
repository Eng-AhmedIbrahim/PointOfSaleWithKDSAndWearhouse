namespace BlazorBase.Models.DeliveryModels;

public class CustomerDetails
{
    public string? FirstPhoneNumber { get; set; }
    public string? SecondPhoneNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? ClientTitle { get; set; }
    public string? HomeNumber { get; set; }
    public string? FloorNumber { get; set; }
    public string? FlatNumber { get; set; }
    public string? BranchName { get; set; }
    public string? ZoneName { get; set; }
    public string? ClientNote { get; set; }
    public string? KitchenNote { get; set; }
    public string? ClientAddress { get; set; }
    public string? OrderDiscount { get; set; }
    public string? AddressNote { get; set; }
    
    // Additional properties for delivery management
    public int CustomerAddressId { get; set; }
    public int ZoneID { get; set; }
    public int Id { get; set; }
    public int BranchId { get; set; }
    public decimal ZoneFees { get; set; }


    public decimal ZoneBonus { get; set; }
    public string? BranchApiUrl { get; set; }

    
    public OrderDto? ActiveOrder { get; set; }
    public List<OrderDto>? Last10Orders { get; set; }

    public static CustomerDetails Clone(CustomerDetails source)
    {
        return new CustomerDetails
        {
            FirstPhoneNumber = source.FirstPhoneNumber,
            SecondPhoneNumber = source.SecondPhoneNumber,
            CustomerName = source.CustomerName,
            ClientTitle = source.ClientTitle,
            HomeNumber = source.HomeNumber,
            FloorNumber = source.FloorNumber,
            FlatNumber = source.FlatNumber,
            BranchName = source.BranchName,
            ZoneName = source.ZoneName,
            ClientNote = source.ClientNote,
            KitchenNote = source.KitchenNote,
            ClientAddress = source.ClientAddress,
            OrderDiscount = source.OrderDiscount,
            AddressNote = source.AddressNote,
            CustomerAddressId = source.CustomerAddressId,
            ZoneID = source.ZoneID,
            Id = source.Id,
            BranchId = source.BranchId,
            ZoneFees = source.ZoneFees,
            ZoneBonus = source.ZoneBonus,
            BranchApiUrl = source.BranchApiUrl,
            ActiveOrder = source.ActiveOrder,

            Last10Orders = source.Last10Orders != null ? new List<OrderDto>(source.Last10Orders) : null
        };
    }
}