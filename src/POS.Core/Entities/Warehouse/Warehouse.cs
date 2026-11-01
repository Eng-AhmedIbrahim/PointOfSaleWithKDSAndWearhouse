namespace POS.Core.Entities.Warehouse;

public enum WarehouseType
{
    Main = 1,    // مخزن رئيسي / مركزي
    Branch = 2   // مخزن تابع لفرع
}

public class Warehouse : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public WarehouseType Type { get; set; } = WarehouseType.Branch;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// رابط API السيرفر الخاص بالمخزن الرئيسي
    /// يُستخدم من الفروع لإرسال الطلبيات إلى المخزن المركزي عبر الإنترنت
    /// مثال: http://100.79.100.86:5050
    /// </summary>
    public string? ApiUrl { get; set; }

    // إذا كان المخزن تابعاً لفرع معين
    public int? BranchId { get; set; }
    public POS.Core.Entities.Company.Branch? Branch { get; set; }

    // المخزن الرئيسي المورد الافتراضي (Default Supplying Warehouse)
    public int? DefaultSupplyingWarehouseId { get; set; }
    public Warehouse? DefaultSupplyingWarehouse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation collections
    public ICollection<WarehouseLocation> Locations { get; set; } = new List<WarehouseLocation>();
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
}