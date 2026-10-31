namespace POS.Contract.Dtos.CategoryDtos;

public class CategoryDto
{
    public int Id { get; set; }
    [Required]
    public string? ArabicName { get; set; }
    [Required]
    public string? EnglishName { get; set; }
    public string? ItemsFont { get; set; }
    public bool Invisible { get; set; } = false;
    public bool IsInventory { get; set; } = false;
    public int? KitchenTypeId { get; set; }
    public bool? PrintInBackupReceipt { get; set; } = true;
    public DateTime CreationDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public CategoryDto()
    {
        CreationDate = DateTime.Now;
        CreationDate.ToString("yyyy-MM-dd hh:mm:ss.fff tt zzz");
    }
}