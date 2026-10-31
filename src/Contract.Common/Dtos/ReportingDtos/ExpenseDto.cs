using System;

namespace POS.Contract.Dtos.ReportingDtos;

public class ExpenseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // e.g., Purchases, Maintenance, Salary
    public string? CreatedById { get; set; }   // Cashier ID
    public string? CreatedByName { get; set; } // Cashier Name
    public string? SpentBy { get; set; }       // Receiver
    public bool IsPayoutFromDrawer { get; set; }
    public int? BranchId { get; set; }
    public int? ShiftId { get; set; }
}
