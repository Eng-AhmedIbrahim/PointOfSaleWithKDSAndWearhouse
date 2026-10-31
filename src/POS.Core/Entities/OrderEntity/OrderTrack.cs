namespace POS.Core.Entities.OrderEntity;

public class OrderTrack : BaseEntity
{
    public int OrderId { get; set; }
    public string? OrderType { get; set; } // DineIn, TakeAway, Delivery
    public string? Action { get; set; } // Created, Updated, ItemAdded, ItemRemoved, DiscountApplied, Voided, Closed, etc.
    public string? UserName { get; set; }
    public string? UserId { get; set; }
    public string? MachineName { get; set; }
    public string? Details { get; set; } // JSON or text details of the action
    public DateTime ActionDateTime { get; set; }
    public int? TableId { get; set; } // For DineIn orders
    public string? TableName { get; set; } // For DineIn orders
}
