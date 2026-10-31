namespace POS.Core.Entities.Delivery;

public class DeliveryOrderInfo:BaseEntity
{
    public int OrderId { get; set; }
    public Orders? Order { get; set; }
    public int? TakerId { get; set; }
    public int? TakerName { get; set; }
    public string? TitleName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Address { get; set; }
    public string? StreetName { get; set; }
    public string? FloorNum { get; set; }
    public string? ApartmentNum { get; set; }
    public string? AddressNotes { get; set; }
    public DateTime? AssignTime { get; set; }
    public DateTime? BackTime { get; set; }
    public int ZoneId { get; set; }
    public DeliveryZone? DeliveryZone { get; set; }
    public DeliveryStates DeliveryStates { get; set; }
    public string? DeliveryState => DeliveryStates.ToString();
}