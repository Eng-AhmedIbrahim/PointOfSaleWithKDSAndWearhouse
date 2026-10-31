using POS.Core.Entities.Delivery;

namespace POS.Core.Entities.ComplaintEntity;

public class Complaint : BaseEntity
{
    public string ComplaintNumber { get; set; } = string.Empty;
    
    // Linking to the existing DeliveryCustomerInfo
    public int? CustomerId { get; set; }
    public DeliveryCustomerInfo? Customer { get; set; }
    
    // Denormalized fields for quick access as requested
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    
    // Explicit OrderId for the complaint
    // Explicit OrderId for the complaint
    public int? OrderId { get; set; }
    public int? OrderDatabaseId { get; set; }
    
    public string ComplaintText { get; set; } = string.Empty;
    public DateTime ComplaintDate { get; set; } = DateTime.Now;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
    public string? Note { get; set; }
    public string? Resolution { get; set; }
    public DateTime? ResolutionDate { get; set; }
}

public enum ComplaintStatus
{
    Open,
    Pending,
    Resolved,
    Closed
}
