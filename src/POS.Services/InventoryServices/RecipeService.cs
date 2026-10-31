using POS.Core.Entities.Item;
using POS.Core.Repository.Contract;
using POS.Core.Services.Contract.InventoryServices;
using POS.Core.Specifications.RecipeSpecs;
using Serilog;

namespace POS.Services.InventoryServices;

public class RecipeService : IRecipeService
{
    private readonly IUnitOfWork _unitOfWork;

    public RecipeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Recipe?> GetRecipeByItemIdAsync(int menuSalesItemId)
    {
        var spec = new RecipeWithIngredientsSpecification(menuSalesItemId);
        return await _unitOfWork.Repository<Recipe>().GetByIdWithSpecificationTrackedAsync(spec);
    }

    public async Task<IReadOnlyList<Recipe>> GetAllRecipesAsync()
    {
        var spec = new RecipeWithIngredientsSpecification();
        return await _unitOfWork.Repository<Recipe>().GetAllWithSpecificationAsync(spec);
    }

    public async Task<Recipe> CreateRecipeAsync(Recipe recipe)
    {
        await _unitOfWork.Repository<Recipe>().AddAsync(recipe);
        await _unitOfWork.CompleteAsync();
        return recipe;
    }

    public async Task<Recipe> UpdateRecipeAsync(Recipe recipe)
    {
        _unitOfWork.Repository<Recipe>().Update(recipe);
        await _unitOfWork.CompleteAsync();
        return recipe;
    }

    public async Task<bool> DeleteRecipeAsync(int id)
    {
        var recipe = await _unitOfWork.Repository<Recipe>().GetByIdAsync(id);
        if (recipe == null) return false;
        
        _unitOfWork.Repository<Recipe>().Delete(recipe);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool> AddIngredientToRecipeAsync(int recipeId, RecipeIngredient ingredient)
    {
        ingredient.RecipeId = recipeId;
        await _unitOfWork.Repository<RecipeIngredient>().AddAsync(ingredient);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool> RemoveIngredientFromRecipeAsync(int ingredientId)
    {
        var ingredient = await _unitOfWork.Repository<RecipeIngredient>().GetByIdAsync(ingredientId);
        if (ingredient == null) return false;
        
        _unitOfWork.Repository<RecipeIngredient>().Delete(ingredient);
        return await _unitOfWork.CompleteAsync() > 0;
    }
}