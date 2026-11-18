namespace POS.Contract.Dtos.InventoryDtos;

public class ProductionRecipeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProducedProductId { get; set; }
    public string ProducedProductName { get; set; } = string.Empty;
    public string? ProducedProductSku { get; set; }
    public decimal YieldQuantity { get; set; } = 1;
    public int? YieldUnitId { get; set; }
    public string? YieldUnitName { get; set; }
    public decimal EstimatedBatchCost { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public List<ProductionRecipeIngredientDto> Ingredients { get; set; } = new();
}

public class ProductionRecipeIngredientDto
{
    public int Id { get; set; }
    public int ProductionRecipeId { get; set; }
    public int IngredientProductId { get; set; }
    public string IngredientProductName { get; set; } = string.Empty;
    public string? IngredientProductSku { get; set; }
    public decimal Quantity { get; set; }
    public int? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal WastePercent { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal CurrentStockAvailable { get; set; }
    public string? Notes { get; set; }
}

public class ProductionOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int ProducedProductId { get; set; }
    public string ProducedProductName { get; set; } = string.Empty;
    public string? ProducedProductSku { get; set; }
    public string? ProducedProductUnitName { get; set; }
    public int? ProductionRecipeId { get; set; }
    public string? ProductionRecipeName { get; set; }
    public decimal TargetQuantity { get; set; }
    public decimal ActualQuantityProduced { get; set; }
    public int Status { get; set; } // 0: Draft, 1: Planned, 2: Completed, 3: Cancelled
    public string StatusText => Status switch
    {
        0 => "مسودة",
        1 => "معتمد للتشغيل",
        2 => "تم الإنتاج والتسليم",
        3 => "ملغي",
        _ => "غير محدد"
    };
    public DateTime OrderDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public decimal TotalActualCost { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByUserName { get; set; }
    public List<ProductionOrderLineDto> Lines { get; set; } = new();
}

public class ProductionOrderLineDto
{
    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public int IngredientProductId { get; set; }
    public string IngredientProductName { get; set; } = string.Empty;
    public string? IngredientProductSku { get; set; }
    public string? UnitName { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantityConsumed { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AvailableStockAtExecution { get; set; }
    public string? Notes { get; set; }
}

public class CreateProductionOrderRequest
{
    public int WarehouseId { get; set; }
    public int ProducedProductId { get; set; }
    public int? ProductionRecipeId { get; set; }
    public decimal TargetQuantity { get; set; } = 1;
    public decimal ActualQuantityProduced { get; set; } = 1;
    public string? Notes { get; set; }
    public string? CreatedByUserName { get; set; }
    public bool AutoExecuteImmediately { get; set; } = true; // تنفيذ وصرف الخامات فوراً
    public List<ProductionOrderLineDto> Lines { get; set; } = new();
}
