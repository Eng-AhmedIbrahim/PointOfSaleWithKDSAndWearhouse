using POS.Contract.Dtos.DineIn;


namespace BlazorBase.Models;

public class VoidItemModel
{
    public OrderItemsDetailsDto? OriginalItem { get; set; }
    public TableItem? DistributionItem { get; set; }
    public TableItem? TableItem { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal OriginalQuantity { get; set; }
    public decimal QuantityToVoid { get; set; }
    public decimal TotalAmount => QuantityToVoid * UnitPrice;
}
