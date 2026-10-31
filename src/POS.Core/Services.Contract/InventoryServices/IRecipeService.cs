using POS.Core.Entities.Item;

namespace POS.Core.Services.Contract.InventoryServices;

public interface IRecipeService
{
    Task<Recipe?> GetRecipeByItemIdAsync(int menuSalesItemId);
    Task<IReadOnlyList<Recipe>> GetAllRecipesAsync();
    Task<Recipe> CreateRecipeAsync(Recipe recipe);
    Task<Recipe> UpdateRecipeAsync(Recipe recipe);
    Task<bool> DeleteRecipeAsync(int id);
    Task<bool> AddIngredientToRecipeAsync(int recipeId, RecipeIngredient ingredient);
    Task<bool> RemoveIngredientFromRecipeAsync(int ingredientId);
}
