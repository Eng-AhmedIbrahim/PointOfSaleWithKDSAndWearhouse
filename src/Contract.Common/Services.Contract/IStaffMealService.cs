using System.Collections.Generic;
using System.Threading.Tasks;
using POS.Contract.Dtos.AccountDtos;

namespace POS.Core.Services.Contract
{
    public interface IStaffMealService
    {
        Task<StaffMealConfigDto?> GetConfigByUserIdAsync(string userId);
        Task<StaffMealStatusDto> GetStatusByUserIdAsync(string userId);
        Task<bool> RecordUsageAsync(StaffMealUsageDto usage);
        Task<IEnumerable<StaffMealConfigDto>> GetAllConfigsAsync();
        Task<bool> UpsertConfigAsync(StaffMealConfigDto config);
        Task<bool> BatchUpsertConfigsAsync(IEnumerable<StaffMealConfigDto> configs);
        
        Task<IEnumerable<StaffMealGroupDto>> GetAllGroupsAsync();
        Task<StaffMealGroupDto?> GetGroupByIdAsync(int groupId);
        Task<bool> UpsertGroupAsync(StaffMealGroupDto group);
        Task<bool> DeleteGroupAsync(int groupId);
    }
}
