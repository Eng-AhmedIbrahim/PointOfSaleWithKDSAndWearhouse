namespace POS.Core.Entities.Warehouse;

public enum PaymentTermType
{
    Cash = 1,              // كاش فوري
    Net15 = 2,             // آجل 15 يوم
    Net30 = 3,             // آجل 30 يوم
    Net45 = 4,             // آجل 45 يوم
    Net60 = 5,             // آجل 60 يوم
    CustomInstallment = 6  // دفعات مخصصة / تقسيط
}

public class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? CommercialRegister { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BankName { get; set; }
    public string? Iban { get; set; }

    public PaymentTermType DefaultPaymentTerm { get; set; } = PaymentTermType.Cash;
    public decimal CreditLimit { get; set; } = 0;
    public decimal CurrentBalance { get; set; } = 0; // الرصيد الحالي للمورد (مديونية)

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SupplierPriceContract> PriceContracts { get; set; } = new List<SupplierPriceContract>();
    public ICollection<SupplierInvoice> Invoices { get; set; } = new List<SupplierInvoice>();
    public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
}

public class SupplierPriceContract : BaseEntity
{
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public int ProductId { get; set; }
    public WarehouseProduct? Product { get; set; }

    public int UnitId { get; set; }
    public WarehouseUnit? Unit { get; set; }

    public decimal AgreedPrice { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
