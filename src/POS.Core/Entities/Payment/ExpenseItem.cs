using System;

namespace POS.Core.Entities.Payment;

public enum ExpenseItemType
{
    General = 0,      // نثريات / عام
    RawMaterial = 1,  // مادة خام
    SalesItem = 2     // صنف بيع
}

public class ExpenseItem : BaseEntity
{
    public int ExpenseId { get; set; }
    public Expense? Expense { get; set; }

    public ExpenseItemType ItemType { get; set; } = ExpenseItemType.General;

    /// <summary>
    /// ID of the item (WarehouseProductId for RawMaterial, or MenuSalesItemId for SalesItem)
    /// </summary>
    public int? ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; } = 0;
    public decimal TotalPrice { get; set; } = 0;

    public string? UnitName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
