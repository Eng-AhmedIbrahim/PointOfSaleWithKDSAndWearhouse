namespace POS.Core.Entities.Item;

public class RecipeIngredient : BaseEntity
{
    public int RecipeId { get; set; }
    public Recipe? Recipe { get; set; }
    
    // Links to an item that is tracked in inventory
    public int MenuSalesIngredientId { get; set; }
    public MenuSalesItems? MenuSalesIngredient { get; set; }
    
    // Amount needed for one unit of the recipe product
    public decimal Quantity { get; set; }
    public int? UnitId { get; set; }
    public Unit? Unit { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
