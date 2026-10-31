namespace POS.Contract.Dtos.CategoryDtos;

public class CreateCategoryDto
{
    public int Id { get; set; }
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public string ItemsFont { get; set; } = "regular";
    public bool Invisible { get; set; } = false;
    public bool IsInventory { get; set; } = false;

    public DateTime CreationDate { get; set; }
}
