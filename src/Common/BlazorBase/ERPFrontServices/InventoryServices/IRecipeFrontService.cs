using POS.Contract.Dtos.InventoryDtos;
using BlazorBase.Helpers;

namespace BlazorBase.ERPFrontServices.InventoryServices;

public interface IRecipeFrontService
{
    Task<ServiceResponse<IReadOnlyList<RecipeDto>>> GetAllRecipesAsync();
    Task<ServiceResponse<RecipeDto>> GetRecipeByItemIdAsync(int itemId);
    Task<ServiceResponse<bool>> CreateRecipeAsync(RecipeDto recipe);
    Task<ServiceResponse<bool>> UpdateRecipeAsync(RecipeDto recipe);
    Task<ServiceResponse<bool>> DeleteRecipeAsync(int id);
}
