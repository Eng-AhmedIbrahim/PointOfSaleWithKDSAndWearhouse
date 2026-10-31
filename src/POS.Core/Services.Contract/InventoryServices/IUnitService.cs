using POS.Core.Entities.Item;

namespace POS.Core.Services.Contract.InventoryServices;

public interface IUnitService
{
    Task<IEnumerable<Unit>> GetAllUnitsAsync();
    Task<Unit?> GetUnitByIdAsync(int id);
    Task<Unit> CreateUnitAsync(Unit unit);
    Task UpdateUnitAsync(Unit unit);
    Task DeleteUnitAsync(int id);
}
