

namespace POS.Contract.Models.ReceiptModels.Delivery;

public class DeliveryReceipt
{
    public string LogoPath { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public int Id { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string ReceiptType { get; set; } = string.Empty;
    public string ReceiptNote { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string FooterMessage { get; set; } = string.Empty;
    private List<ReceiptItem> _items = [];
    public IReadOnlyList<ReceiptItem> Items => _items;
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;
    public float LogoWidth { get; set; } = 120f;
    public bool IsFollowUp { get; set; }
    public int? ParentOrderId { get; set; }
    public int? RemoteOrderId { get; set; }
    public bool IsCopy { get; set; }
    public bool IsVoid { get; set; }


    //Customer Information
    public string? CustomerFirstPhone { get; set; }
    public string? CustomerSecondPhone { get; set; }
    public string? CustomerName { get; set; }
    public string? Building { get; set; }
    public string? HomeNumber { get; set; }
    public string? FloorNumber { get; set; }
    public string? FlatNumber { get; set; }
    public string? ZoneName { get; set; }
    public string? AddressNote { get; set; }
    public string? DeliveryName { get; set; }
    public string? DeliveryDisplayName { get; set; }

    //Amount
    public decimal? TotalAmount { get; set; }
    public decimal? DeliveryFees { get; set; }
    public decimal? TotalOrder { get; set; }
    public string? CustomerAddress { get; set; }

    // Hospitality & Staff Meals
    public bool IsHospitality { get; set; }
    public string? HospitalityResponsibleName { get; set; }
    public string? HospitalityReason { get; set; }
    public bool IsStaffMeal { get; set; }
    public string? StaffMealEmployeeName { get; set; }

    public void AddItem(ReceiptItem item) => _items.Add(item);
    public void AddItems(IEnumerable<ReceiptItem> items) => _items.AddRange(items);
}
