namespace POS.Contract.Dtos.ItemDto;

public class MenuSalesItemsToReturnDto
{
    public int Id { get; set; }
    public string? Barcode { get; set; }
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public decimal? Price { get; set; }
    public decimal? ExtraPrice { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? BackColor { get; set; }
    public string? TextColor { get; set; }
    public int? TextSize { get; set; }
    public bool Invisible { get; set; } = false;
    public int CategoryId { get; set; }
    public int? MainCategoryId { get; set; }
    public bool? PrintInBackupReceipt { get; set; }
    public bool? PrintInBackupReceiptFromCategory { get; set; }
    public int? KitchenTypeId { get; set; }
    public int? CategoryKitchenTypeId { get; set; }
    public decimal? Tax { get; set; }
    public int? BranchId { get; set; }
    public string? CategoryArabicName { get; set; }
    public string? CategoryEnglishName { get; set; }
    public List<MenuSalesItemAttributes> Attributes { get; set; } = [];
    public bool HasAttribute { get; set; } = false;
    public int? AttributeId { get; set; }
    public bool ByWeight { get; set; } = false;
    public bool IsInventory { get; set; } = true;
    public int? ItemTypeId { get; set; }
}