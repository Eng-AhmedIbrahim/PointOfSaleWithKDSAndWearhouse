namespace POS.Services.KitchenServices;

public class KitchenServices : IKitchenServices
{
    private readonly IUnitOfWork _unitOfWork;

    public KitchenServices(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<KitchenType>> GetAllKitchenTypesAsync()
    {
        try
        {
            var specs = new KitchenTypeSpecs();
            var kitchenTypes = await _unitOfWork.Repository<KitchenType>().GetAllWithSpecificationAsync(specs);

            return kitchenTypes ?? new List<KitchenType>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving all Kitchen Types.");
            return new List<KitchenType>();
        }
    }

    public async Task<KitchenType?> GetKitchenTypeByIdAsync(int id)
    {
        try
        {
            var specs = new KitchenTypeSpecs(id);
            var kitchenType = await _unitOfWork.Repository<KitchenType>().GetByIdWithSpecificationAsync(specs);

            return kitchenType;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving Kitchen Type with Id {id}", id);
            return null;
        }
    }

    public async Task<KitchenType?> CreateKitchenTypeAsync(KitchenType kitchenType)
    {
        try
        {
            if (kitchenType == null)
                return null;

            await _unitOfWork.Repository<KitchenType>().AddAsync(kitchenType);
            var result = await _unitOfWork.CompleteAsync();
            return result > 0 ? kitchenType : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating Kitchen Type.");
            return null;
        }
    }

    public async Task<bool> UpdateKitchenTypeAsync(KitchenType newkitchenType)
    {
        try
        {
            if (newkitchenType == null)
                return false;

            var exists = await _unitOfWork.Repository<KitchenType>().GetByIdAsync(newkitchenType.Id);
            if (exists == null)
                return false;

            if (newkitchenType.BranchId != exists.BranchId)
                exists.BranchId = newkitchenType.BranchId;

            if (!string.IsNullOrEmpty(newkitchenType.KitchenName))
                exists.KitchenName = newkitchenType.KitchenName;

            _unitOfWork.Repository<KitchenType>().Update(exists);
            var result = await _unitOfWork.CompleteAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating Kitchen Type with Id {id}", newkitchenType.Id);
            return false;
        }
    }

    public async Task<bool> DeleteKitchenTypeAsync(int id)
    {
        try
        {
            var kitchenType = await _unitOfWork.Repository<KitchenType>().GetByIdAsync(id);
            if (kitchenType == null)
                return false;

            // Delete related KitchenPrinters first to avoid FK constraint violation
            var allPrinters = await _unitOfWork.Repository<KitchenPrinters>().GetAllAsync();
            var relatedPrinters = allPrinters.Where(kp => kp.KitchenTypeId == id).ToList();

            if (relatedPrinters.Any())
            {
                foreach (var printer in relatedPrinters)
                    _unitOfWork.Repository<KitchenPrinters>().Delete(printer);

                await _unitOfWork.CompleteAsync();
            }

            // Unlink related Categories and Items to prevent FK constraint violation
            var allCategories = await _unitOfWork.Repository<Category>().GetAllAsync();
            var relatedCategories = allCategories.Where(c => c.KitchenTypeId == id).ToList();
            if (relatedCategories.Any())
            {
                foreach (var cat in relatedCategories)
                {
                    cat.KitchenTypeId = null;
                    _unitOfWork.Repository<Category>().Update(cat);
                }
                await _unitOfWork.CompleteAsync();
            }

            var allItems = await _unitOfWork.Repository<MenuSalesItems>().GetAllAsync();
            var relatedItems = allItems.Where(i => i.KitchenTypeId == id).ToList();
            if (relatedItems.Any())
            {
                foreach (var item in relatedItems)
                {
                    item.KitchenTypeId = null;
                    _unitOfWork.Repository<MenuSalesItems>().Update(item);
                }
                await _unitOfWork.CompleteAsync();
            }

            _unitOfWork.Repository<KitchenType>().Delete(kitchenType);
            var result = await _unitOfWork.CompleteAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting Kitchen Type with Id {id}", id);
            return false;
        }
    }

    public async Task<KitchenType?> GetKitchenWithSpecificationAsync(ISpecifications<KitchenType> specification)
    {
        if(specification is null)
            return null;

        try
        {
            var result = await _unitOfWork.Repository<KitchenType>().GetByIdWithSpecificationAsync(specification);
            if (result  is null)
                return null;

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving  Kitchen Types.");
            return null;
        }
    }
}