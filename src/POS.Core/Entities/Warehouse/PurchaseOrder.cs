namespace POS.Core.Entities.Warehouse;

public enum PurchaseOrderStatus
{
    Draft = 1,
    Approved = 2,
    PartiallyReceived = 3,
    FullyReceived = 4,
    Cancelled = 5
}

public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty; // e.g. PO-2026-0001
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int WarehouseId { get; set; } // المخزن المستلم (الرئيسي)
    public Warehouse? Warehouse { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
    public ICollection<GoodsReceiptNote> GoodsReceiptNotes { get; set; } = new List<GoodsReceiptNote>();
}

public class PurchaseOrderLine : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int ProductId { get; set; }
    public WarehouseProduct? Product { get; set; }

    public int UnitId { get; set; }
    public WarehouseUnit? Unit { get; set; }

    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; } = 0;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => OrderedQuantity * UnitPrice;
}

public class GoodsReceiptNote : BaseEntity
{
    public string GrnNumber { get; set; } = string.Empty; // e.g. GRN-2026-0001
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
    public string? SupplierInvoiceNumber { get; set; }
    public string? ReceivedByUserId { get; set; }
    public string? Notes { get; set; }

    public ICollection<GoodsReceiptNoteLine> Lines { get; set; } = new List<GoodsReceiptNoteLine>();
}

public class GoodsReceiptNoteLine : BaseEntity
{
    public int GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote? GoodsReceiptNote { get; set; }

    public int ProductId { get; set; }
    public WarehouseProduct? Product { get; set; }

    public int UnitId { get; set; }
    public WarehouseUnit? Unit { get; set; }

    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class SupplierInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int? GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote? GoodsReceiptNote { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    public bool IsFullyPaid => RemainingAmount <= 0;
}

public class SupplierPayment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash, BankTransfer, Cheque
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
}
