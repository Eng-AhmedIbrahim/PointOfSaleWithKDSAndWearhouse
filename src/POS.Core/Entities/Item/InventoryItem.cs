namespace POS.Core.Entities.Item;

/// <summary>
/// Represents a product's inventory configuration and current stock level.
/// </summary>
public class InventoryItem : BaseEntity
{
    public int MenuSalesItemId { get; set; }
    public MenuSalesItems? MenuSalesItem { get; set; }

    /// <summary>Current quantity available in stock.</summary>
    public decimal CurrentQuantity { get; set; }

    /// <summary>Minimum threshold that triggers a low-stock warning.</summary>
    public decimal MinimumQuantity { get; set; }

    /// <summary>Unit of measure.</summary>
    public int? UnitId { get; set; }
    public Unit? Unit { get; set; }

    /// <summary>Whether this item's stock is actively tracked.</summary>
    public bool TrackInventory { get; set; } = true;

    /// <summary>Branch this inventory record belongs to.</summary>
    public int? BranchId { get; set; }

    // Denormalized fields for quick access/sync
    public string? ItemNameAr { get; set; }
    public string? ItemNameEn { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryNameAr { get; set; }
    public string? CategoryNameEn { get; set; }
    public string? UnitNameAr { get; set; }
    public string? UnitNameEn { get; set; }
    public int? ItemTypeId { get; set; }
    public string? ItemTypeCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}
