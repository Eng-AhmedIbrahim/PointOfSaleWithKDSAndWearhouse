using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.OrderEntity;

public class OrderVoidItem : BaseEntity
{
    public int OrderVoidId { get; set; }
    public int OrderDetailId { get; set; }
    
    // Quantity tracking
    public decimal QuantityBefore { get; set; }
    public decimal QuantityVoided { get; set; }
    public decimal QuantityAfter { get; set; }

    // Amount tracking
    public decimal AmountBefore { get; set; }
    public decimal AmountVoided { get; set; }
    public decimal AmountAfter { get; set; }

    public string? Reason { get; set; }

    // Navigation properties
    [ForeignKey("OrderVoidId")]
    public OrderVoid? OrderVoid { get; set; }

    [ForeignKey("OrderDetailId")]
    public OrderItemsDetails? OrderItem { get; set; }
}
