using System;
using System.Collections.Generic;

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
    public List<ExpenseItemDto> Items { get; set; } = new();
}

public class ExpenseItemDto
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public int ItemType { get; set; } // 0: General, 1: RawMaterial, 2: SalesItem
    public int? ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? UnitName { get; set; }
    public string? Notes { get; set; }
}
