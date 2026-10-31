using Microsoft.AspNetCore.Mvc;
using POS.Contract.Dtos.AccountDtos;
using POS.Core.Services.Contract;

namespace POS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StaffMealController : ControllerBase
    {
        private readonly IStaffMealService _staffMealService;

        public StaffMealController(IStaffMealService staffMealService)
        {
            _staffMealService = staffMealService;
        }

        [HttpGet("config/{userId}")]
        public async Task<ActionResult<StaffMealConfigDto>> GetConfig(string userId)
        {
            var config = await _staffMealService.GetConfigByUserIdAsync(userId);
            if (config == null) return NotFound();
            return Ok(config);
        }

        [HttpGet("status/{userId}")]
        public async Task<ActionResult<StaffMealStatusDto>> GetStatus(string userId)
        {
            var status = await _staffMealService.GetStatusByUserIdAsync(userId);
            return Ok(status);
        }

        [HttpPost("usage")]
        public async Task<ActionResult<bool>> RecordUsage(StaffMealUsageDto usage)
        {
            var success = await _staffMealService.RecordUsageAsync(usage);
            return Ok(success);
        }

        [HttpGet("configs")]
        public async Task<ActionResult<IEnumerable<StaffMealConfigDto>>> GetAllConfigs()
        {
            var configs = await _staffMealService.GetAllConfigsAsync();
            return Ok(configs);
        }

        [HttpPost("upsert")]
        public async Task<ActionResult<bool>> UpsertConfig(StaffMealConfigDto config)
        {
            var success = await _staffMealService.UpsertConfigAsync(config);
            return Ok(success);
        }

        [HttpPost("batch-upsert")]
        public async Task<ActionResult<bool>> BatchUpsertConfigs([FromBody] IEnumerable<StaffMealConfigDto> configs)
        {
            var success = await _staffMealService.BatchUpsertConfigsAsync(configs);
            return Ok(success);
        }

        [HttpGet("groups")]
        public async Task<ActionResult<IEnumerable<StaffMealGroupDto>>> GetAllGroups()
        {
            var groups = await _staffMealService.GetAllGroupsAsync();
            return Ok(groups);
        }

        [HttpGet("group/{id}")]
        public async Task<ActionResult<StaffMealGroupDto>> GetGroupById(int id)
        {
            var group = await _staffMealService.GetGroupByIdAsync(id);
            if (group == null) return NotFound();
            return Ok(group);
        }

        [HttpPost("group")]
        public async Task<ActionResult<bool>> UpsertGroup(StaffMealGroupDto group)
        {
            var success = await _staffMealService.UpsertGroupAsync(group);
            return Ok(success);
        }

        [HttpDelete("group/{id}")]
        public async Task<ActionResult<bool>> DeleteGroup(int id)
        {
            var success = await _staffMealService.DeleteGroupAsync(id);
            return Ok(success);
        }
    }
}
