namespace POS.Core.Entities.OrderEntity;

public class OrderItemComment : BaseEntity
{
    public int OrderItemDetailId { get; set; }
    public OrderItemsDetails? OrderItemDetail { get; set; }
    
    public string? Comment { get; set; }
    public DateTime CommentTime { get; set; } = DateTime.Now;
    public string? AddedBy { get; set; }
}
