namespace BlazorBase.Models;

public class DineInOrderDetails
{
    public int DatabaseId { get; set; }
    public string OrderType { get; } = "DineIn";
    public OrderDetails? BasicOrderDetails { get; set; } = new();

    public int? RelatedTableId { get; set; }
    public string? RelatedTableName { get; set; }
    public string? CaptainId { get; set; }
    public string? CaptainName { get; set; }
    public int? PrintCount { get; set; }

    public DineInOrderDetails Clone()
    {
        return new DineInOrderDetails
        {
            DatabaseId = this.DatabaseId,
            RelatedTableId = this.RelatedTableId,
            RelatedTableName = this.RelatedTableName,
            CaptainId = this.CaptainId,
            CaptainName = this.CaptainName,
            PrintCount = this.PrintCount,
            BasicOrderDetails = this.BasicOrderDetails?.Clone()
        };
    }
    public bool IsMigrated { get; set; } = false;
    public List<int> MergedTableIds { get; set; } = new();


}
