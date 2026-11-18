namespace POS.Core.Entities.Warehouse;

public enum ProductionOrderStatus
{
    Draft = 0,      // مسودة
    Planned = 1,    // معتمد للتنفيذ
    Completed = 2,  // تم التصنيع وتسليم المخزن
    Cancelled = 3   // ملغي
}

public class ProductionRecipe : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    // الصنف المصنّع الناتج
    public int ProducedProductId { get; set; }
    public virtual WarehouseProduct? ProducedProduct { get; set; }

    // الكمية الناتجة من الوصفة (مثلاً لإنتاج 10 كغ أو 1 كغ)
    public decimal YieldQuantity { get; set; } = 1;
    public int? YieldUnitId { get; set; }
    public virtual WarehouseUnit? YieldUnit { get; set; }

    public decimal EstimatedBatchCost { get; set; } = 0;
    public decimal EstimatedUnitCost { get; set; } = 0;

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ProductionRecipeIngredient> Ingredients { get; set; } = new List<ProductionRecipeIngredient>();
}

public class ProductionRecipeIngredient : BaseEntity
{
    public int ProductionRecipeId { get; set; }
    public virtual ProductionRecipe? ProductionRecipe { get; set; }

    // الخامة المطلوبة الداخلة في التصنيع
    public int IngredientProductId { get; set; }
    public virtual WarehouseProduct? IngredientProduct { get; set; }

    // الكمية المطلوبة من الخامة لكل Batch
    public decimal Quantity { get; set; } = 0;
    public int? UnitId { get; set; }
    public virtual WarehouseUnit? Unit { get; set; }

    // نسبة الهالك المتوقعة في التصنيع (مثلاً 5%)
    public decimal WastePercent { get; set; } = 0;
    public decimal EstimatedCost { get; set; } = 0;

    public string? Notes { get; set; }
}

public class ProductionOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty; // PRD-202611-0001
    
    // المخزن المركزي الذي يتم فيه الإنتاج وخصم الخامات وإيداع الصنف المصنع
    public int WarehouseId { get; set; }
    public virtual Warehouse? Warehouse { get; set; }

    // الصنف المصنع
    public int ProducedProductId { get; set; }
    public virtual WarehouseProduct? ProducedProduct { get; set; }

    public int? ProductionRecipeId { get; set; }
    public virtual ProductionRecipe? ProductionRecipe { get; set; }

    // الكمية المستهدفة والكمية الفعلية المنتجة
    public decimal TargetQuantity { get; set; } = 1;
    public decimal ActualQuantityProduced { get; set; } = 1;

    public ProductionOrderStatus Status { get; set; } = ProductionOrderStatus.Draft;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }

    // تكاليف التشغيل الفعلية
    public decimal TotalActualCost { get; set; } = 0;
    public decimal UnitCost { get; set; } = 0;

    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }

    public virtual ICollection<ProductionOrderLine> Lines { get; set; } = new List<ProductionOrderLine>();
}

public class ProductionOrderLine : BaseEntity
{
    public int ProductionOrderId { get; set; }
    public virtual ProductionOrder? ProductionOrder { get; set; }

    public int IngredientProductId { get; set; }
    public virtual WarehouseProduct? IngredientProduct { get; set; }

    // الكمية المخططة حسب الريسيبي
    public decimal PlannedQuantity { get; set; } = 0;

    // الكمية المستهلكة فعلياً في أمر التشغيل
    public decimal ActualQuantityConsumed { get; set; } = 0;

    public decimal UnitCost { get; set; } = 0;
    public decimal TotalCost { get; set; } = 0;

    // الرصيد الذي كان متاحاً في المخزن لحظة البدء للتتبع
    public decimal AvailableStockAtExecution { get; set; } = 0;

    public string? Notes { get; set; }
}
