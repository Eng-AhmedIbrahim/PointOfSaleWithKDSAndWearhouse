namespace POS.Contract.Dtos.InventoryDtos;

public class RecipeDto
{
    public int Id { get; set; }
    public int MenuSalesItemId { get; set; }
    public string? MenuSalesItemName { get; set; }
    public string? RecipeName { get; set; }
    public bool IsActive { get; set; }
    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
}

public class RecipeIngredientDto
{
    public int Id { get; set; }
    public int MenuSalesIngredientId { get; set; }
    public string? IngredientName { get; set; }
    public decimal Quantity { get; set; }
    public int? UnitId { get; set; }
    public string? Unit { get; set; }
}
