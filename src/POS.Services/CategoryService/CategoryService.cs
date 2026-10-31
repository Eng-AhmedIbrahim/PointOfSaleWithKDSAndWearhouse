using POS.Core.Entities.Categorie;
using POS.Core.Entities.Item;
using POS.Core.Specifications;
using POS.Core.Specifications.InventorySpecs;

namespace POS.Services.CategoryService;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Category?>? CreateCategoryAsync(Category category)
    {
        try
        {
            if (category is null)
                return null;

           await  _unitOfWork.Repository<Category>().AddAsync(category);  

            var result = await _unitOfWork.CompleteAsync();
            if(result <= 0)
                return null;

            return category;
        }
        catch (Exception ex) 
        {
            Log.Error(ex, "Error During Creating Category");
            return null;
        }
    }

    public async Task<bool> DeleteCategory(Category category)
    {
        try
        {
            if (category is null)
                return false;

            _unitOfWork.Repository<Category>().Delete(category);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return false;

            return true;

        }catch (Exception ex) 
        {
            Log.Error(ex, "Error Occur During Deleting Category That Has Id {catId}", category.Id);
            return false;
        }
    }

    public async Task<IReadOnlyList<Category>?> GetCategoriesAsync()
    {
        try
        {
            var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
            if (!categories.Any())
                return null;

            return categories;
        }catch(Exception ex)
        {
            Log.Error(ex,"There Are An Error During Get All Categories {error}",ex.Message);
            return null;
        }
    }

    public async Task<Category?> GetCategoryByIdAsync(int catId)
    {
        try
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(catId);
            if (category is null)
                return null;

            return category;    
        }catch(Exception ex)
        {
            Log.Error(ex, "Error Occur During Getting Category With Id {catid} ", catId);
            return null;
        }
    }

    public async Task<Category?> UpdateCategory(Category oldCategory, Category newCategory)
    {
        try
        {
            oldCategory.Id = oldCategory.Id;

            if (!string.IsNullOrEmpty(newCategory.ArabicName))
                oldCategory.ArabicName = newCategory.ArabicName;

            if (!string.IsNullOrEmpty(newCategory.EnglishName))
            {
                oldCategory.EnglishName = newCategory.EnglishName;
                oldCategory.NormalizedEnglishName = newCategory.EnglishName.ToUpper();
            }

            if (!string.IsNullOrEmpty(newCategory.ItemsFont))
                oldCategory.ItemsFont = newCategory.ItemsFont;

            if (newCategory.Invisible != oldCategory.Invisible)
                oldCategory.Invisible = newCategory.Invisible;

            if (newCategory.KitchenTypeId != oldCategory.KitchenTypeId)
                oldCategory.KitchenTypeId = newCategory.KitchenTypeId;

            if (newCategory.PrintInBackupReceipt.HasValue && newCategory.PrintInBackupReceipt != oldCategory.PrintInBackupReceipt)
                oldCategory.PrintInBackupReceipt = newCategory.PrintInBackupReceipt;

            if (newCategory.IsInventory != oldCategory.IsInventory)
                oldCategory.IsInventory = newCategory.IsInventory;

             _unitOfWork.Repository<Category>().Update(oldCategory);
            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;

            return oldCategory ?? null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Occur During Update Category That Have Id {companyId}", oldCategory.Id);
            return null;
        }
    }

}