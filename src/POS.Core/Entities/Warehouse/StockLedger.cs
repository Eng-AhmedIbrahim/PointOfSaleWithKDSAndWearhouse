namespace POS.Core.Entities.Warehouse;

public enum StockMovementType
{
    GoodsReceipt = 1,        // وارد من مورد (GRN)
    SupplierReturn = 2,      // مرتجع لمورد
    BranchIssue = 3,         // صرف لفرع (طلب فرع معتمد)
    BranchReceipt = 4,       // استلام في مخزن الفرع
    TransferOut = 5,         // خروج تحويل مخزني
    TransferIn = 6,          // دخول تحويل مخزني
    InventoryAdjustment = 7, // تسوية جرد (عجز أو زيادة)
    WasteDamage = 8          // تالف أو هالك
}

public class StockLedger : BaseEntity
{
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int ProductId { get; set; }
    public WarehouseProduct? Product { get; set; }

    public int? LocationId { get; set; }
    public WarehouseLocation? Location { get; set; }

    public StockMovementType MovementType { get; set; }

    // الكميات بالوحدة الأساسية (Base Unit) للحفاظ على دقة الحسابات المطلقة
    public decimal QuantityIn { get; set; } = 0;
    public decimal QuantityOut { get; set; } = 0;
    public decimal RunningBalance { get; set; } = 0;

    // تكلفة الوحدة والتكلفة الإجمالية للحركة
    public decimal UnitCost { get; set; } = 0;
    public decimal TotalCost { get; set; } = 0;

    // المرجع (رقم إذن الاستلام، رقم الطلبية، رقم التحويل)
    public string ReferenceDocumentType { get; set; } = string.Empty;
    public string ReferenceDocumentNumber { get; set; } = string.Empty;

    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
}

public class StockBalance : BaseEntity
{
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int ProductId { get; set; }
    public WarehouseProduct? Product { get; set; }

    // الرصيد الفعلي المتاح بالوحدة الأساسية
    public decimal QuantityOnHand { get; set; } = 0;

    // الكميات المحجوزة للطلبيات المعتمدة قيد التجهيز
    public decimal QuantityReserved { get; set; } = 0;

    // الكميات التي في الطريق (In-Transit)
    public decimal QuantityInTransit { get; set; } = 0;

    // الرصيد المتاح للطلب = الفعلي - المحجوز
    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;

    public decimal WeightedAverageCost { get; set; } = 0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
