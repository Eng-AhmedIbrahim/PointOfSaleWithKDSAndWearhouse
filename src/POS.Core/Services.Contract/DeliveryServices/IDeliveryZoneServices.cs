namespace POS.Core.Services.Contract.DeliveryServices;

public interface IDeliveryZoneServices
{
    public Task<IEnumerable<DeliveryZone>> GetAllZonesAsync();
    public Task<IReadOnlyList<DeliveryZone>> GetZonesByBranchAsync(int branchId);
    public Task<DeliveryZone?> GetZoneByIdAsync(int id);
    public Task<DeliveryZone> CreateZoneAsync(DeliveryZone zone);
    public Task<bool> UpdateZoneAsync(DeliveryZone zone);
    public Task<bool> DeleteZoneAsync(int id);
}