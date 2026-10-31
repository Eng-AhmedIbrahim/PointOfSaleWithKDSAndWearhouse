using POS.Core.Specifications.DeliverySpecs;

namespace POS.Services.DeliveryServices;

public class DeliveryZoneServices : IDeliveryZoneServices
{
    private readonly IUnitOfWork _unitOfWork;

    public DeliveryZoneServices(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DeliveryZone>> GetAllZonesAsync()
    {
        var zones = await _unitOfWork.Repository<DeliveryZone>().GetAllAsync();
        return zones;
    }

    public async Task<IReadOnlyList<DeliveryZone>> GetZonesByBranchAsync(int branchId)
    {
        DeliveryZonesSpecs zonesSpecs = new DeliveryZonesSpecs(branchId);

        var zones = await _unitOfWork.Repository<DeliveryZone>().GetAllWithSpecificationAsync(zonesSpecs);
        return zones!;
    }

    public async Task<DeliveryZone?> GetZoneByIdAsync(int id)
    {
        var zone = await _unitOfWork.Repository<DeliveryZone>().GetByIdAsync(id);
        return zone;
    }

    public async Task AddZoneAsync(DeliveryZone zone)
    {
        await _unitOfWork.Repository<DeliveryZone>().AddAsync(zone);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<bool> UpdateZoneAsync(DeliveryZone zone)
    {
        var existingZone = await _unitOfWork.Repository<DeliveryZone>().GetByIdAsync(zone.Id);
        if (existingZone != null)
        {
            existingZone.ZoneName = zone.ZoneName;
            existingZone.DeliveryFee = zone.DeliveryFee;
            existingZone.ZoneBonus = zone.ZoneBonus;
            existingZone.BranchId = zone.BranchId;

            _unitOfWork.Repository<DeliveryZone>().Update(existingZone);
            await _unitOfWork.CompleteAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteZoneAsync(int id)
    {
        var zone = await _unitOfWork.Repository<DeliveryZone>().GetByIdAsync(id);
        if (zone != null)
        {
            _unitOfWork.Repository<DeliveryZone>().Delete(zone);
            await _unitOfWork.CompleteAsync();
            return true;
        }
        return false;
    }

    public async Task<DeliveryZone> CreateZoneAsync(DeliveryZone zone)
    {
        await _unitOfWork.Repository<DeliveryZone>().AddAsync(zone);

        await _unitOfWork.CompleteAsync();

        return zone;
    }
}