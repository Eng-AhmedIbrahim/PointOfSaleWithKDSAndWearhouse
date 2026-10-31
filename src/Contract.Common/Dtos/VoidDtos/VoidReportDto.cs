namespace POS.Contract.Dtos.VoidDtos;

/// <summary>
/// Represents a full void report entry - one per voided order
/// </summary>
public class VoidReportDto
{
    public int OrderDbId { get; set; }       // Database PK
    public int OrderId { get; set; }          // Display order number
    public string? OrderType { get; set; }
    public string? OrderState { get; set; }
    public DateTime? OrderDate { get; set; }

    // Who placed the order
    public string? CashierName { get; set; }
    public string? CustomerName { get; set; }
    public string? Phone { get; set; }

    // DineIn specifics
    public int? TableId { get; set; }
    public string? TableName { get; set; }
    public string? WaiterName { get; set; }

    // Delivery specifics
    public string? DriverName { get; set; }
    public string? BranchName { get; set; }
    public int? BranchId { get; set; }

    // Void details
    public string? VoidBy { get; set; }
    public string? VoidByName { get; set; }
    public DateTime? VoidTime { get; set; }
    public string? VoidReason { get; set; }
    public decimal? VoidCount { get; set; }         // Number of void operations on this order

    // Financial
    public decimal? OriginalTotal { get; set; }  // Grand total before void
    public decimal? VoidedAmount { get; set; }   // Total amount voided (TotalVoid)
    public decimal? RemainingTotal { get; set; } // What's left (GrandTotal after)
    public bool IsFullyVoided { get; set; }

    // Voided items detail
    public List<VoidItemReportDto> VoidedItems { get; set; } = new();
}

/// <summary>
/// Represents a single voided item line in the void report
/// </summary>
public class VoidItemReportDto
{
    public int OrderDetailId { get; set; }
    public string? ItemName { get; set; }
    public string? ItemNameAr { get; set; }
    public string? CategoryName { get; set; }
    public decimal? UnitPrice { get; set; }

    // Void quantities
    public decimal VoidedQuantity { get; set; }    // How many were voided
    public decimal RemainingQuantity { get; set; } // Active quantity left
    public decimal? VoidedValue { get; set; }  // Total value of voided qty
    public bool IsFullyVoided { get; set; }

    // Who/when/why this item was voided
    public string? VoidByName { get; set; }
    public DateTime? VoidTime { get; set; }
    public string? VoidReason { get; set; }
}
