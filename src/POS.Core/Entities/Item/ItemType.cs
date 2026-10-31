namespace POS.Core.Entities.Item;

public class ItemType : BaseEntity
{
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public string? Code { get; set; } // e.g., "SaleItem", "RawMaterial"
    
    public ICollection<MenuSalesItems> MenuSalesItems { get; set; } = new List<MenuSalesItems>();
}
