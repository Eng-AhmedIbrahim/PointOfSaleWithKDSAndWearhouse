using POS.Contract;

namespace BlazorBase.ERPFrontServices.CategoryServices;

public interface ICategoryServices
{
    public Task<ICollection<CategoryToReturnDto>> GetAllCategoriesAsync();

    public Task<ICollection<MenuSalesItemsToReturnDto>> GetItemsByCategoryIdAsync(int categoryId);
    public Task<ServiceResponse<IReadOnlyList<CategoryToReturnDto>>> GetAllCategories();
    public Task<ServiceResponse<CategoryToReturnDto>> GetCategoryById(int categoryId);
    public Task<ServiceResponse<CategoryToReturnDto>> CreateCategory(CreateCategoryDto newCategory);
    public Task<ServiceResponse<CategoryToReturnDto>> UpdateCategory(UpdatedCategoryDto updatedCategory);
    public Task<ServiceResponse<bool>> DeleteCategory(int categoryId);
}