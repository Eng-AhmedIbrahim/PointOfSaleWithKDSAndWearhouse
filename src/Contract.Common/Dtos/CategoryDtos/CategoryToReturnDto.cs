namespace POS.Contract.Dtos.CategoryDtos;

public class CategoryToReturnDto
{
    public int Id { get; set; }
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public string? ItemsFont { get; set; }
    public bool Invisible { get; set; } = false;
    public bool IsInventory { get; set; } = false;
    public bool? PrintInBackupReceipt { get; set; }
    public int? KitchenTypeId { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}