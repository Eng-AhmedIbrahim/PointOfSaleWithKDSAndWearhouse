namespace POS.Contract.Dtos.ItemDto;

public class MenuSalesItemsDto
{
    public int Id { get; set; }
    public string? Barcode { get; set; }
    [Required]
    public string? ArabicName { get; set; }
    [Required]
    public string? EnglishName { get; set; }
    public decimal? Price { get; set; }
    public decimal? SecondPrice { get; set; }
    public decimal? ThirdPrice { get; set; }
    public decimal? FourthPrice { get; set; }
    public decimal? FifthPrice { get; set; }
    public decimal? Tax { get; set; }
    public string? Description { get; set; }
    public IFormFile? Image { get; set; }
    public int? MainCategoryId { get; set; }
    public string? BackColor { get; set; }
    public string? TextColor { get; set; }
    public int? TextSize { get; set; } // px or pt
    public bool Invisible { get; set; } = false;
    public int? ItemTypeId { get; set; } = 1;
    public DateTime CreationDate { get; set; }

    public int? BranchId { get; set; } = 1;
    [Required]
    public int? CategoryId { get; set; }
    public bool HasAttribute { get; set; } = false;
    public int? AttributeId { get; set; }
    public int? KitchenTypeId { get; set; }
    public bool? PrintInBackupReceipt { get; set; } = true;
    public bool ByWeight { get; set; } = false;
    public bool IsInventory { get; set; } = true;
    public MenuSalesItemsDto()
    {
        CreationDate = DateTime.Now;
        CreationDate.ToString("yyyy-MM-dd hh:mm:ss.fff tt zzz");
    }
}