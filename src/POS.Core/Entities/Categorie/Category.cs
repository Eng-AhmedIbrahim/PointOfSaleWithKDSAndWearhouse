namespace POS.Core.Entities.Categorie;

public class Category : BaseEntity
{
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public string? NormalizedEnglishName { get; set; }
    public string? ItemsFont { get; set; }
    public bool Invisible { get; set; } = false;
    public bool IsInventory { get; set; } = false;
    public bool? PrintInBackupReceipt { get; set; } = true;

    public DateTime CreationDate { get; set; }
    public DateTime? UpdateDate { get; set; }
   
    public int BranchId { get; set; } = 1;
    public Branch? Branch { get; set; }

    public int? KitchenTypeId { get; set; }
    public KitchenType? KitchenType { get; set; }

    public ICollection<MenuSalesItems> MenuSalesItems { get; set; } = [];
}