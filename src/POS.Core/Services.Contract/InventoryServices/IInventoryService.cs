namespace POS.Core.Services.Contract.InventoryServices;
using POS.Core.Entities.Item;

public interface IInventoryService
{
    Task<InventoryItem?> GetInventoryByItemIdAsync(int menuSalesItemId);
    Task<IReadOnlyList<InventoryItem>> GetAllInventoryItemsAsync();
    
    Task<bool> UpdateStockAsync(int menuSalesItemId, decimal quantityChange, TransactionType type, string? referenceId = null, string? notes = null, string? reason = null, string? imagePaths = null, string? createdBy = null);
    
    Task<bool> SetOpeningStockAsync(int menuSalesItemId, decimal openingQuantity, string? notes = null, string? createdBy = null);
    Task<bool> SetPhysicalStockAsync(int menuSalesItemId, decimal actualQuantity, string? notes = null, string? createdBy = null);
    
    Task ConsumeItemStockAsync(int menuSalesItemId, decimal quantity, TransactionType type = TransactionType.Sale, string? referenceId = null);
    
    Task<InventoryItem> InitializeInventoryAsync(int menuSalesItemId, decimal initialQuantity, decimal minQuantity, int? unitId, bool? trackInventory = null);
    Task InitializeAllItemsAsync();
    
    Task<IReadOnlyList<InventoryTransaction>> GetTransactionsByItemIdAsync(int menuSalesItemId);
}
