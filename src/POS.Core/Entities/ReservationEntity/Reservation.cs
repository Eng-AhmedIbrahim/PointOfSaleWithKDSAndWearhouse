using POS.Core.Entities.DineIn;
using POS.Core.Entities.OrderEntity;

namespace POS.Core.Entities.ReservationEntity;

public class Reservation : BaseEntity
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
    public int? GuestCount { get; set; }
    public int? MaleCount { get; set; }
    public int? FemaleCount { get; set; }
    public int? TableId { get; set; }
    public int BranchId { get; set; }
    public decimal? ReservationPaid { get; set; }
    public string? ReservationStatus { get; set; } // Pending, Confirmed, Seated, Cancelled
    public string? Notes { get; set; }
    
    // Link to Order
    public int? OrderId { get; set; }
    public Orders? Order { get; set; }
    
    // Link to Table
    public Table? Table { get; set; }
}
