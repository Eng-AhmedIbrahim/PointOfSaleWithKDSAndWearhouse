namespace POS.Core.Entities.Delivery;

public class Dispatch:BaseEntity
{
    public int BranchID { get; set; } = 1;
    public DateTime? OrderDate { get; set; }
    public string? UserID { get; set; } 
    public string? UserName { get; set; } 
    public string? DriverID { get; set; }
    public string? DriverName { get; set; } 
    public int? OrderID { get; set; }
    public Orders? Order { get; set; }
}