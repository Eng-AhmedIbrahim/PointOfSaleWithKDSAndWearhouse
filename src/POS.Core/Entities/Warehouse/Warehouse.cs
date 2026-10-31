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

    // في حالة كان مخزن فرع، يرتبط بالفرع التابع له
    public int? BranchId { get; set; }
    public POS.Core.Entities.Company.Branch? Branch { get; set; }

    // المخزن المغذي الافتراضي (Default Supplying Warehouse)
    public int? DefaultSupplyingWarehouseId { get; set; }
    public Warehouse? DefaultSupplyingWarehouse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation collections
    public ICollection<WarehouseLocation> Locations { get; set; } = new List<WarehouseLocation>();
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
}
