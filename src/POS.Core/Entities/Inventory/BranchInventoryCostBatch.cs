using System;

namespace POS.Core.Entities.Inventory;

/// <summary>
/// Represents a cost layer/batch of raw materials or retail products received in the branch.
/// Priority determines consumption order (e.g., 1 for local expense purchases, 2 for warehouse transfers).
/// </summary>
public class BranchInventoryCostBatch : BaseEntity
{
    public int BranchId { get; set; } = 1;

    /// <summary>MenuSalesItemId in POS (links to raw material or retail item)</summary>
    public int MenuSalesItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    /// <summary>"ExpensePurchase" (local purchase from drawer) or "WarehouseTransfer" (from central warehouse)</summary>
    public string BatchSource { get; set; } = string.Empty;

    /// <summary>e.g. EXP-00015 or REQ-00042</summary>
    public string ReferenceId { get; set; } = string.Empty;

    public decimal OriginalQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? UnitName { get; set; }

    /// <summary>1 = ExpensePurchase (consumed first), 2 = WarehouseTransfer</summary>
    public int Priority { get; set; } = 2;

    public bool IsExhausted { get; set; } = false;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
