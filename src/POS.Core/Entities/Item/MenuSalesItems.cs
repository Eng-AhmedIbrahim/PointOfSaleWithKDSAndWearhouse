namespace POS.Core.Entities.Item;

public class MenuSalesItems : BaseEntity
{
    public string? Barcode { get; set; }
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public string? NormalizedEnglishName { get; set; }
    public decimal? Price { get; set; }
    public decimal? SecondPrice { get; set; }
    public decimal? ThirdPrice { get; set; }
    public decimal? FourthPrice { get; set; }
    public decimal? FifthPrice { get; set; }
    public decimal? Tax { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public int? MainCategoryId { get; set; }
    public ItemsClassifications? MainCategory { get; set; }
    public string? BackColor { get; set; }
    public string? TextColor { get; set; }
    public int? TextSize { get; set; }
    public bool Invisible { get; set; } = false;
    public int? ItemTypeId { get; set; } = 1;
    public ItemType? ItemType { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }
    
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public bool HasAttribute { get; set; } = false;
    public int? AttributeId { get; set; }
    public Attributes? Attribute { get; set; }

    public ICollection<OrderItemsDetails>? OrderDetails { get; set; } = new List<OrderItemsDetails>();
    public int? KitchenTypeId { get; set; }
    public KitchenType? KitchenType { get; set; }
    public bool? PrintInBackupReceipt { get; set; }
    public bool IsInventory { get; set; } = true;
    public bool ByWeight { get; set; } = false;
}