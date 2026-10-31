namespace POS.Core.Entities.Kitchen;

public class KitchenPrinters : BaseEntity
{
    public string? Copy1 { get; set; }
    public string? Copy2 { get; set; }
    public string? Copy3 { get; set; }
    public string? Copy4 { get; set; }
    public string? Copy5 { get; set; }
    public string? Copy6 { get; set; }
    public string? Copy7 { get; set; }
    public string? Copy8 { get; set; }
    public string? Copy9 { get; set; }
    public string? Copy10 { get; set; }
    public string? DeviceName { get; set; }

    public int? KitchenTypeId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public KitchenType? KitchenType { get; set; }
}