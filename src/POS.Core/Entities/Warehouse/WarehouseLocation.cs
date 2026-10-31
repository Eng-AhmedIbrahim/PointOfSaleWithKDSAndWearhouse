namespace POS.Core.Entities.Warehouse;

public class WarehouseLocation : BaseEntity
{
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Zone { get; set; }   // منطقة / ممر
    public string? Shelf { get; set; }  // رف
    public string? Bin { get; set; }    // صندوق / خانة
    public bool IsActive { get; set; } = true;
}
