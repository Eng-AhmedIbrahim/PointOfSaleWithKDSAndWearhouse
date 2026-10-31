namespace POS.Core.Entities.Item;

public class Recipe : BaseEntity
{
    public int MenuSalesItemId { get; set; }
    public MenuSalesItems? MenuSalesItem { get; set; }
    
    public string? RecipeName { get; set; }
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
