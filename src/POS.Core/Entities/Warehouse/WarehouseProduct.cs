namespace POS.Core.Entities.Warehouse;

public class WarehouseUnit : BaseEntity
{
    public string NameAr { get; set; } = string.Empty; // كيلو، كرتونة، لتر، حبة
    public string NameEn { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty; // kg, ctn, L, pcs
    public bool IsBaseUnit { get; set; } = false;
}

public class WarehouseProduct : BaseEntity
{
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CategoryName { get; set; }

    // ربط الصنف بالفئة المخزنية المستقلة
    public int? CategoryId { get; set; }
    public WarehouseCategory? Category { get; set; }

    // الوحدة الأساسية للتخزين والحسابات في الـ Stock Ledger (مثل كغ أو لتر أو قطعة)
    public int BaseUnitId { get; set; }
    public WarehouseUnit? BaseUnit { get; set; }

    // التكلفة القياسية وتكلفة المتوسط المرجح الحالية
    public decimal WeightedAverageCost { get; set; } = 0;
    public decimal LastPurchaseCost { get; set; } = 0;

    // حدود المخزون للتنبيهات
    public decimal MinStockLevel { get; set; } = 0;
    public decimal ReorderPoint { get; set; } = 0;
    public decimal ReorderQuantity { get; set; } = 0;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // وحدات القياس المتعددة للصنف
    public ICollection<WarehouseProductUnit> ProductUnits { get; set; } = new List<WarehouseProductUnit>();
    public ICollection<StockBalance> Balances { get; set; } = new List<StockBalance>();
}

public class WarehouseProductUnit : BaseEntity
{
    public int ProductId { get; set; }
    public WarehouseProduct? Product { get; set; }

    public int UnitId { get; set; }
    public WarehouseUnit? Unit { get; set; }

    // معامل التحويل إلى الوحدة الأساسية (مثلاً الكرتونة = 12 لتر)
    public decimal ConversionFactorToBase { get; set; } = 1;
    public bool IsDefaultPurchaseUnit { get; set; } = false;
    public bool IsDefaultRequisitionUnit { get; set; } = false;
}
