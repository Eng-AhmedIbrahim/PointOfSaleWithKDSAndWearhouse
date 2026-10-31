using POS.Contract.Dtos.InventoryDtos;

namespace BlazorBase.ERPFrontServices.InventoryServices;

public interface IInventoryFrontService
{
    Task<ServiceResponse<IReadOnlyList<InventoryItemDto>>> GetAllInventoryAsync();
    Task<ServiceResponse<InventoryItemDto>> GetInventoryByItemIdAsync(int itemId);
    Task<ServiceResponse<bool>> UpdateStockAsync(UpdateStockDto updateDto);
    Task<ServiceResponse<bool>> SetOpeningStockAsync(UpdateStockDto updateDto);
    Task<ServiceResponse<bool>> SetPhysicalStockAsync(UpdateStockDto updateDto);
    Task<ServiceResponse<bool>> InitializeInventoryAsync(InventoryItemDto initDto);
    Task<ServiceResponse<bool>> InitializeAllItemsAsync();
    Task<ServiceResponse<IReadOnlyList<InventoryTransactionDto>>> GetTransactionsAsync(int itemId);
}
