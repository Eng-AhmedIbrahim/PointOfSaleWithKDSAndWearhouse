namespace POS.Services.ItemServices;

public class MenuSalesItemService : IMenuSalesItemService
{
    private readonly IUnitOfWork _unitOfWork;

    public MenuSalesItemService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MenuSalesItems?> CreateItemAsync(MenuSalesItems item)
    {
        try
        {
            if (item is null)
                return null;

            await _unitOfWork.Repository<MenuSalesItems>().AddAsync(item);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;

            // Automatically create the initial InventoryItem record to ensure stock deduction happens
            if (item.IsInventory)
            {
                var inventory = new InventoryItem
                {
                    MenuSalesItemId = item.Id,
                    CurrentQuantity = 0,
                    MinimumQuantity = 0,
                    TrackInventory = item.IsInventory,
                    CreatedAt = DateTime.UtcNow,
                    BranchId = item.BranchId,
                    ItemTypeId = item.ItemTypeId
                };
                await _unitOfWork.Repository<InventoryItem>().AddAsync(inventory);
                
                // --- UPWARD SYNC ---
                // If item is inventory tracked, ensure its category is also marked as inventory tracked
                if (item.CategoryId.HasValue)
                {
                    var category = await _unitOfWork.Repository<Category>().GetByIdAsync(item.CategoryId.Value);
                    if (category != null && !category.IsInventory)
                    {
                        category.IsInventory = true;
                        _unitOfWork.Repository<Category>().Update(category);
                    }
                }
                
                await _unitOfWork.CompleteAsync();
            }

            return item;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating Item.");
            return null;
        }
    }
    public async Task<bool> DeleteItem(MenuSalesItems item)
    {
        try
        {
            if (item is null)
                return false;

            _unitOfWork.Repository<MenuSalesItems>().Delete(item);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return false;

            return true;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cant Delete Company With Id {companyId}", item.Id);
            return false;
        }
    }
    public async Task<IReadOnlyList<MenuSalesItems>?> GetAllItemsAsync()
    {
        try
        {
            var itemSpecs = new MenuSalesItemsWithIncludeSpec();
            var items = await _unitOfWork.Repository<MenuSalesItems>().GetAllWithSpecificationAsync(itemSpecs);

            if (items is null)
                return null;

            return items;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Items");
            return null;
        }
    }

    public async Task<IReadOnlyList<ItemsClassifications>?> GetAllClassificationsAsync()
    {
        try
        {
            var classifications = await _unitOfWork.Repository<ItemsClassifications>().GetAllAsync();
            return classifications;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading items classifications");
            return null;
        }
    }

    public async Task<ItemsClassifications?> GetClassificationByIdAsync(int id)
    {
        try
        {
            return await _unitOfWork.Repository<ItemsClassifications>().GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting classification by id {id}", id);
            return null;
        }
    }

    public async Task<ItemsClassifications?> CreateClassificationAsync(ItemsClassifications classification)
    {
        try
        {
            await _unitOfWork.Repository<ItemsClassifications>().AddAsync(classification);
            var result = await _unitOfWork.CompleteAsync();
            return result > 0 ? classification : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating classification");
            return null;
        }
    }

    public async Task<ItemsClassifications?> UpdateClassificationAsync(ItemsClassifications oldClassification, ItemsClassifications newClassification)
    {
        try
        {
            if (!string.IsNullOrEmpty(newClassification.ArabicName))
                oldClassification.ArabicName = newClassification.ArabicName;
            if (!string.IsNullOrEmpty(newClassification.Name))
                oldClassification.Name = newClassification.Name;

            _unitOfWork.Repository<ItemsClassifications>().Update(oldClassification);
            var result = await _unitOfWork.CompleteAsync();
            return result > 0 ? oldClassification : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating classification {id}", oldClassification.Id);
            return null;
        }
    }

    public async Task<bool> DeleteClassification(ItemsClassifications classification)
    {
        try
        {
            _unitOfWork.Repository<ItemsClassifications>().Delete(classification);
            var result = await _unitOfWork.CompleteAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting classification {id}", classification.Id);
            return false;
        }
    }

    public async Task<MenuSalesItems?> GetItemByIdAsync(int itemId)
    {
        try
        {
            var itemSpecs = new MenuSalesItemsWithIncludeSpec(itemId);
            var item = await _unitOfWork.Repository<MenuSalesItems>().GetByIdWithSpecificationAsync(itemSpecs);

            // Fallback: if spec-based lookup returns null (e.g. item has no Attribute), try simple lookup
            if (item is null)
                item = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(itemId);

            return item;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Item With This Id {itemId}", itemId);
            return default;
        }
    }
    public async Task<MenuSalesItems?> UpdateItemAsync(MenuSalesItems oldItem, MenuSalesItems newItem)
    {
        try
        {
            // NOTE: Do NOT set oldItem.Id from newItem.Id — newItem is mapped from UpdatedItemDto
            // which uses 'ItemId', so newItem.Id would be 0 after mapping. oldItem already has the correct Id.

            if (!string.IsNullOrEmpty(newItem.ArabicName))
                oldItem.ArabicName = newItem.ArabicName;
            if (!string.IsNullOrEmpty(newItem.EnglishName))
                oldItem.EnglishName = newItem.EnglishName;

            if (newItem.Price != oldItem.Price || newItem.Price != null)
                oldItem.Price = newItem.Price;

            if (newItem.SecondPrice != oldItem.SecondPrice || newItem.SecondPrice != null)
                oldItem.SecondPrice = newItem.SecondPrice;

            if (newItem.ThirdPrice != oldItem.ThirdPrice || newItem.ThirdPrice != null)
                oldItem.ThirdPrice = newItem.ThirdPrice;

            if (newItem.FourthPrice != oldItem.FourthPrice || newItem.FourthPrice != null)
                oldItem.FourthPrice = newItem.FourthPrice;

            if (newItem.FifthPrice != oldItem.FifthPrice || newItem.FifthPrice != null)
                oldItem.FifthPrice = newItem.FifthPrice;

            if (!string.IsNullOrEmpty(newItem.Description))
                oldItem.Description = newItem.Description;

            if (newItem.CategoryId != oldItem.CategoryId || newItem.CategoryId != null)
                oldItem.CategoryId = newItem.CategoryId;

            if (newItem.Tax != oldItem.Tax || newItem.Tax != null)
                oldItem.Tax = newItem.Tax;

            if (!string.IsNullOrEmpty(newItem.BackColor))
                oldItem.BackColor = newItem.BackColor;

            if (!string.IsNullOrEmpty(newItem.TextColor))
                oldItem.TextColor = newItem.TextColor;

            if (!string.IsNullOrEmpty(newItem.MainCategoryId.ToString()))
                oldItem.MainCategoryId = newItem.MainCategoryId;

            if (!string.IsNullOrEmpty(newItem.Barcode))
                oldItem.Barcode = newItem.Barcode;

            if (newItem.TextSize != oldItem.TextSize || newItem.TextSize != null)
                oldItem.TextSize = newItem.TextSize;

            if (!string.IsNullOrEmpty(newItem.ImagePath))
                oldItem.ImagePath = newItem.ImagePath;

            if (newItem.Invisible != oldItem.Invisible)
                oldItem.Invisible = newItem.Invisible;

            if (newItem.ItemTypeId != oldItem.ItemTypeId)
                oldItem.ItemTypeId = newItem.ItemTypeId;

            if (newItem.KitchenTypeId != oldItem.KitchenTypeId)
                oldItem.KitchenTypeId = newItem.KitchenTypeId;

            if (newItem.PrintInBackupReceipt.HasValue && newItem.PrintInBackupReceipt != oldItem.PrintInBackupReceipt)
                oldItem.PrintInBackupReceipt = newItem.PrintInBackupReceipt;
            
            if (newItem.ByWeight != oldItem.ByWeight)
                oldItem.ByWeight = newItem.ByWeight;

            if (newItem.IsInventory != oldItem.IsInventory)
                oldItem.IsInventory = newItem.IsInventory;

            // Clear navigation properties to prevent EF from syncing the related entities
            // and overriding the modified foreign keys (e.g. CategoryId).
            oldItem.Category = null;
            oldItem.MainCategory = null;
            oldItem.ItemType = null;
            oldItem.KitchenType = null;
            oldItem.Attribute = null;
            oldItem.Branch = null;
            oldItem.OrderDetails = null;

            _unitOfWork.Repository<MenuSalesItems>().Update(oldItem);
            await _unitOfWork.CompleteAsync();

            // Sync InventoryItem.TrackInventory with the item's IsInventory flag
            var invSpec = new InventoryItemByMenuSalesItemIdSpecification(oldItem.Id);
            var inventoryItem = await _unitOfWork.Repository<InventoryItem>().GetByIdWithSpecificationTrackedAsync(invSpec);

            if (inventoryItem == null && oldItem.IsInventory)
            {
                // No inventory record exists yet – create one so deductions can fire
                var newInventory = new InventoryItem
                {
                    MenuSalesItemId = oldItem.Id,
                    CurrentQuantity = 0,
                    MinimumQuantity = 0,
                    TrackInventory = true,
                    CreatedAt = DateTime.UtcNow,
                    BranchId = oldItem.BranchId,
                    ItemTypeId = oldItem.ItemTypeId
                };
                await _unitOfWork.Repository<InventoryItem>().AddAsync(newInventory);
                
                // --- UPWARD SYNC ---
                // If item is inventory tracked, ensure its category is also marked as inventory tracked
                if (oldItem.CategoryId.HasValue)
                {
                    var category = await _unitOfWork.Repository<Category>().GetByIdAsync(oldItem.CategoryId.Value);
                    if (category != null && !category.IsInventory)
                    {
                        category.IsInventory = true;
                        _unitOfWork.Repository<Category>().Update(category);
                    }
                }
                
                await _unitOfWork.CompleteAsync();
            }
            else if (inventoryItem != null && (inventoryItem.TrackInventory != oldItem.IsInventory))
            {
                // Record exists but TrackInventory is out of sync – fix it
                inventoryItem.TrackInventory = oldItem.IsInventory;
                _unitOfWork.Repository<InventoryItem>().Update(inventoryItem);
                
                // --- UPWARD SYNC (on update) ---
                if (oldItem.IsInventory && oldItem.CategoryId.HasValue)
                {
                    var category = await _unitOfWork.Repository<Category>().GetByIdAsync(oldItem.CategoryId.Value);
                    if (category != null && !category.IsInventory)
                    {
                        category.IsInventory = true;
                        _unitOfWork.Repository<Category>().Update(category);
                    }
                }
                
                await _unitOfWork.CompleteAsync();
            }

            return oldItem;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Occur During Update Item That Have Id {itemId}", oldItem.Id);
            return null;
        }
    }
    public async Task<MenuSalesItems?> AddAttributeToItem(int attributeId, int itemId)
    {
        try
        {
            var item = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(itemId);
            if (item is null)
                return null;

            item.AttributeId = attributeId > 0 ? attributeId : null;
            item.HasAttribute = attributeId > 0;

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0 && attributeId > 0) return null;

            var itemSpecs = new MenuSalesItemsWithIncludeSpec(itemId);
            var itemWithAttribute = await _unitOfWork.Repository<MenuSalesItems>().GetByIdWithSpecificationAsync(itemSpecs);

            //if(itemWithAttribute is null)
            //    return null;

            return itemWithAttribute;

        }catch(Exception ex)
        {
            Log.Error(ex, "Error Ocurr During Add Attribute To Item That Has Id {itemId}",itemId);
            return null;
        }
    }

    public async Task<IReadOnlyList<MenuSalesItems>?> GetItemByCategoryIdAsync(int catId)
    {
        try
        {
            var itemSpecs = new MenuSalesItemsWithIncludeSpec(c => c.CategoryId == catId);
            var items = await _unitOfWork.Repository<MenuSalesItems>().GetAllWithSpecificationAsync(itemSpecs);


            if (items == null) return null;

            return items;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return null;
        }
    }
    public async Task<IReadOnlyList<ItemType>?> GetAllItemTypesAsync()
    {
        try
        {
            return await _unitOfWork.Repository<ItemType>().GetAllAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading item types");
            return null;
        }
    }
}