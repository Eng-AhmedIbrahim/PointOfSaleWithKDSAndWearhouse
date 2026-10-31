using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.Payment;

public class Expense : BaseEntity
{
    public DateTime Date { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; } // e.g., Purchases, Maintenance, Salary, Utilities
    public string? CreatedById { get; set; }   // Cashier ID
    public string? CreatedByName { get; set; } // Cashier Name
    public string? SpentBy { get; set; }       // The person the money was given to
    public string? MachineName { get; set; }
    public int? BranchId { get; set; }   // Linked to branch
    public int? ShiftId { get; set; }    // Linked to shift if taken from drawer
    
    // Flag to indicate if this was taken from the cash drawer
    public bool IsPayoutFromDrawer { get; set; } = true;
}
