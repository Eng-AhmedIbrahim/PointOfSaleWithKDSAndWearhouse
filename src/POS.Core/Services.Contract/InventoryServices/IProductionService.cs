using POS.Contract.Dtos.InventoryDtos;

namespace POS.Core.Services.Contract.InventoryServices;

public interface IProductionService
{
    Task<List<ProductionOrderDto>> GetProductionOrdersAsync(int? warehouseId = null);
    Task<ProductionOrderDto?> GetProductionOrderByIdAsync(int id);
    Task<ProductionOrderDto> CreateProductionOrderAsync(CreateProductionOrderRequest request);
    Task<bool> CompleteProductionOrderAsync(int id, decimal actualProducedQty, string? userId = null);
    Task<bool> CancelProductionOrderAsync(int id, string? reason = null);
    
    Task<List<ProductionRecipeDto>> GetRecipesAsync();
    Task<ProductionRecipeDto?> GetRecipeByProductIdAsync(int productId);
    Task<ProductionRecipeDto> SaveRecipeAsync(ProductionRecipeDto dto);
    Task<bool> DeleteRecipeAsync(int id);

    Task<List<ProductionRecipeIngredientDto>> CalculateRequiredIngredientsAsync(int warehouseId, int productId, decimal targetQty);
}
