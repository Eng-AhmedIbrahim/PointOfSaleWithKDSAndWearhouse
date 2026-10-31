namespace POS.Contract.Dtos.ItemDto;

public class UpdatedItemDto
{
    [Required]
    public int ItemId { get; set; }
    public string? Barcode { get; set; }
    public string? ArabicName { get; set; }
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
    public int? ItemTypeId { get; set; }
    public DateTime UpdatedDate { get; set; }

    public int? BranchId { get; set; } = 1;
    public int? CategoryId { get; set; }
    public bool HasAttribute { get; set; } = false;
    public int? AttributeId { get; set; }
    public int? KitchenTypeId { get; set; }
    public bool? PrintInBackupReceipt { get; set; }
    public bool ByWeight { get; set; } = false;
    public bool IsInventory { get; set; } = true;

    public UpdatedItemDto()
    {
        UpdatedDate = DateTime.Now;
        UpdatedDate.ToString("yyyy-MM-dd hh:mm:ss.fff tt zzz");
    }
}
