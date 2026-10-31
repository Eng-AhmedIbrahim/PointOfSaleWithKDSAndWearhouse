namespace POS.Contract.Models.ReceiptModels.DineIn;

public class DineInReceipt
{
    public int Id { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public string CaptainName { get; set; } = string.Empty;
    public string ReceiptType { get; set; } = string.Empty;
    public string ReceiptNote { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string FooterMessage { get; set; } = string.Empty;
    public string LogoPath { get; set; } = "F:\\Point Of Sale\\PointOfSale\\src\\ERPFront\\wwwroot\\images\\Logo.png";
    private List<ReceiptItem> _items = [];
    public IReadOnlyList<ReceiptItem> Items => _items;
    public decimal? TotalAmount { get; set; } = 100;
    public string ServiceAmount { get; set; } = string.Empty;
    public string TaxAmount { get; set; } = string.Empty;
    public decimal? Discount { get; set; }
    public string TotalOrder { get; set; } = string.Empty;
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;
    public float LogoWidth { get; set; } = 120f;
    public bool IsFollowUp { get; set; }
    public bool IsCopy { get; set; }
    public int PrintCount { get; set; }
    public bool IsVoid { get; set; }
    public bool IsClosing { get; set; }

    // Hospitality & Staff Meals
    public bool IsHospitality { get; set; }
    public string? HospitalityResponsibleName { get; set; }
    public string? HospitalityReason { get; set; }
    public bool IsStaffMeal { get; set; }
    public string? StaffMealEmployeeName { get; set; }

    public void AddItem(ReceiptItem item) => _items.Add(item);
    public void AddItems(IEnumerable<ReceiptItem> items) => _items.AddRange(items);
}

