using POS.Contract.Dtos.InventoryDtos;

namespace BlazorBase.ERPFrontServices.InventoryServices;

public interface IUnitFrontService
{
    Task<ServiceResponse<IEnumerable<UnitDto>>> GetAllUnitsAsync();
    Task<ServiceResponse<UnitDto>> GetUnitByIdAsync(int id);
    Task<ServiceResponse<bool>> CreateUnitAsync(UnitDto unit);
    Task<ServiceResponse<bool>> UpdateUnitAsync(UnitDto unit);
    Task<ServiceResponse<bool>> DeleteUnitAsync(int id);
}
