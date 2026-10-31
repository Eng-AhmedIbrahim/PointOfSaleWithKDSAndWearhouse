namespace POS.Core.Entities.Item;

public enum TransactionType
{
    StockIn = 10,        // Manual addition
    StockOut = 20,       // Manual removal
    Sale = 30,           // Automatic deduction from sales
    Void = 35,           // Return stock after voiding order
    Adjustment = 40,     // Manual correction
    Damage = 50,         // Stock removal due to damage
    Waste = 55,          // Recorded as waste (not returned to stock)
    OpeningStock = 60,   // Initial quantity when starting inventory
    PhysicalCount = 70   // Formal stock take / count
}

public class InventoryTransaction : BaseEntity
{
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public decimal QuantityChange { get; set; } // Positive for addition, negative for deduction
    public decimal ResultingQuantity { get; set; }
    
    public TransactionType Type { get; set; }
    public string? ReferenceId { get; set; } // OrderId or BatchId
    public string? Reason { get; set; }      // Why it happened (e.g. Expired, Broken)
    public virtual ICollection<InventoryTransactionImage> Images { get; set; } = new List<InventoryTransactionImage>();
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
