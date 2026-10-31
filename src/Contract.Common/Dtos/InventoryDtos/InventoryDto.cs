namespace POS.Contract.Dtos.InventoryDtos;

public class InventoryItemDto
{
    public int Id { get; set; }
    public int MenuSalesItemId { get; set; }
    public string? ItemNameAr { get; set; }
    public string? ItemNameEn { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal MinimumQuantity { get; set; }
    public int? UnitId { get; set; }
    public string? Unit { get; set; }
    public string? UnitNameAr { get; set; }
    public string? UnitNameEn { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryNameAr { get; set; }
    public string? CategoryNameEn { get; set; }
    public bool TrackInventory { get; set; }
    public int? ItemTypeId { get; set; }
    public string? ItemTypeCode { get; set; }
    public bool HasRecipe { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateStockDto
{
    public int MenuSalesItemId { get; set; }
    public decimal QuantityChange { get; set; }
    public int TransactionType { get; set; } // Map to TransactionType enum
    public string? Reason { get; set; }
    public string? ImagePaths { get; set; } // Comma separated or JSON
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
}

public class InventoryTransactionDto
{
    public int Id { get; set; }
    public decimal QuantityChange { get; set; }
    public decimal ResultingQuantity { get; set; }
    public string? Type { get; set; }
    public string? ReferenceId { get; set; }
    public string? Reason { get; set; }
    public string? ImagePaths { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
