namespace POS.Core.Entities.Item;

public class OrderItemAttributes :BaseEntity
{
    public int OrderItemId { get; set; }
    public OrderItemsDetails? OrderItem { get; set; }

    public int? AttributeItemId { get; set; }
    public AttributeItem? AttributeItem { get; set; }

    public string AttributeName { get; set; } = string.Empty;
    public decimal? ExtraPrice { get; set; } = 0;
}
