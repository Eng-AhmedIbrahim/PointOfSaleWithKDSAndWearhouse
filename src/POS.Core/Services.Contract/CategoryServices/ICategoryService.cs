using POS.Core.Entities.Categorie;

namespace POS.Core.Services.Contract.CategoryServices;

public interface ICategoryService
{
    public Task<Category?>? CreateCategoryAsync(Category category);
    public Task<IReadOnlyList<Category>?> GetCategoriesAsync();
    public Task<Category?> GetCategoryByIdAsync(int catId);
    public Task<Category?> UpdateCategory(Category oldCategory, Category newCategory);
    public Task<bool> DeleteCategory(Category category);
}
