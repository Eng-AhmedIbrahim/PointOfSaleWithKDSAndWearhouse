using POS.Contract;

namespace BlazorBase.ERPFrontServices.ItemServices;

public interface IItemService
{
    Task<ServiceResponse<IReadOnlyList<MenuSalesItemsToReturnDto>>> GetAllItemsAsync();
    Task<ServiceResponse<IReadOnlyList<ItemsClassificationsDto>>> GetAllClassificationsAsync();
    Task<ServiceResponse<ItemsClassificationsDto>> CreateClassificationAsync(ItemsClassificationsDto newClassification);
    Task<ServiceResponse<ItemsClassificationsDto>> UpdateClassificationAsync(ItemsClassificationsDto updatedClassification);
    Task<ServiceResponse<bool>> DeleteClassificationAsync(int classificationId);
    Task<ServiceResponse<MenuSalesItemsToReturnDto>> GetItemByIdAsync(int itemId);
    Task<ServiceResponse<MenuSalesItemsToReturnDto>> CreateItemAsync(MenuSalesItemsDto newItem);
    Task<ServiceResponse<MenuSalesItemsToReturnDto>> UpdateItemAsync(UpdatedItemDto updatedItem);
    Task<ServiceResponse<bool>> DeleteItemAsync(int itemId);
    Task<ServiceResponse<MenuSalesItemsToReturnDto>> AddAttributeToItemAsync(int attributeId, int itemId);
    Task<ServiceResponse<IReadOnlyList<ItemTypeDto>>> GetItemTypesAsync();
}
