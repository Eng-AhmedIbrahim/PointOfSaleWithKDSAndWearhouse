namespace BackOffice.Desktop.Models;

public class VoidItemModel
{
    public OrderItemsDetailsDto? OriginalItem { get; set; }
    public TableItem? DistributionItem { get; set; }
    public TableItem? TableItem { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int OriginalQuantity { get; set; }
    public int QuantityToVoid { get; set; }
    public decimal TotalAmount => QuantityToVoid * UnitPrice;
}
