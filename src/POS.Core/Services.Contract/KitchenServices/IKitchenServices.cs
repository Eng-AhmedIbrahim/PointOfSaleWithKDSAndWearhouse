namespace POS.Core.Services.Contract.KitchenServices;

public interface IKitchenServices
{
    public Task<IReadOnlyList<KitchenType>> GetAllKitchenTypesAsync();
    public Task<KitchenType?> GetKitchenTypeByIdAsync(int id);
    public Task<KitchenType?> GetKitchenWithSpecificationAsync(ISpecifications<KitchenType> specification);
    public Task<KitchenType?> CreateKitchenTypeAsync(KitchenType kitchenType);
    public Task<bool> UpdateKitchenTypeAsync(KitchenType kitchenType);
    public Task<bool> DeleteKitchenTypeAsync(int id);
}