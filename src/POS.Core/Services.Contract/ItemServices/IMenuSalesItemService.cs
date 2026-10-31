namespace POS.Core.Services.Contract.ItemServices;

public interface IMenuSalesItemService
{
    public Task<MenuSalesItems?> CreateItemAsync(MenuSalesItems item);
    public Task<MenuSalesItems?> UpdateItemAsync(MenuSalesItems oldItem, MenuSalesItems newItem);
    public Task<MenuSalesItems?> GetItemByIdAsync(int itemId);
    public Task<IReadOnlyList<MenuSalesItems>?> GetItemByCategoryIdAsync(int catId);
    public Task<bool> DeleteItem(MenuSalesItems item);
    public Task<IReadOnlyList<MenuSalesItems>?> GetAllItemsAsync();
    public Task<IReadOnlyList<ItemsClassifications>?> GetAllClassificationsAsync();
    public Task<ItemsClassifications?> GetClassificationByIdAsync(int id);
    public Task<ItemsClassifications?> CreateClassificationAsync(ItemsClassifications classification);
    public Task<ItemsClassifications?> UpdateClassificationAsync(ItemsClassifications oldClassification, ItemsClassifications newClassification);
    public Task<bool> DeleteClassification(ItemsClassifications classification);
    public Task<MenuSalesItems?> AddAttributeToItem(int attributeId,int itemId);
    public Task<IReadOnlyList<ItemType>?> GetAllItemTypesAsync();
}