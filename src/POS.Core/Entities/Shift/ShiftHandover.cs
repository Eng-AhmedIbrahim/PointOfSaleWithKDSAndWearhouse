namespace POS.Core.Entities.Shift;

public class ShiftHandover :BaseEntity
{
    public int BranchID { get; set; } = 1;
    public DateTime? WorkingDate { get; set; } 
    public DateTime? StartShiftTime { get; set; } 
    public DateTime? EndShiftTime { get; set; } 
    public int? ShiftNumber { get; set; } 
    public string? FromCashierID { get; set; }
    public string? FromCashierName { get; set; } 
    public string? ToCashierID { get; set; } 
    public string? ToCashierName { get; set; } 
    public decimal? Amount { get; set; }
    public List<Orders>? Orders { get; set; }
}