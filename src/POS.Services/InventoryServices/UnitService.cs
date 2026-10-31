using POS.Core.Entities.Item;
using POS.Core.Services.Contract.InventoryServices;

namespace POS.Services.InventoryServices;

public class UnitService : IUnitService
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Unit>> GetAllUnitsAsync()
    {
        return await _unitOfWork.Repository<Unit>().GetAllAsync();
    }

    public async Task<Unit?> GetUnitByIdAsync(int id)
    {
        return await _unitOfWork.Repository<Unit>().GetByIdAsync(id);
    }

    public async Task<Unit> CreateUnitAsync(Unit unit)
    {
        await _unitOfWork.Repository<Unit>().AddAsync(unit);
        await _unitOfWork.CompleteAsync();
        return unit;
    }

    public async Task UpdateUnitAsync(Unit unit)
    {
        _unitOfWork.Repository<Unit>().Update(unit);
        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteUnitAsync(int id)
    {
        var unit = await GetUnitByIdAsync(id);
        if (unit != null)
        {
            _unitOfWork.Repository<Unit>().Delete(unit);
            await _unitOfWork.CompleteAsync();
        }
    }
}
