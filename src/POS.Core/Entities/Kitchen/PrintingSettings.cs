namespace POS.Core.Entities.Kitchen;

public class PrintingSettings: BaseEntity
{
    public int BranchID { get; set; }
    public string? OrderType { get; set; }
    public string? Copy1 { get; set; }
    public string? Copy2 { get; set; }
    public string? Copy3 { get; set; }
    public string? Copy4 { get; set; }
    public string? Copy5 { get; set; }
    public string? ComputerName { get; set; }
    public int? KitchenId { get; set; }
    public KitchenType? Kitchen { get; set; }
    public string? OutLetType { get; set; }
}