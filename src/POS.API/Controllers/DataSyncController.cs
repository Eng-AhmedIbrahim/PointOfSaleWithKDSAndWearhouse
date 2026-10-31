using Microsoft.AspNetCore.Mvc;
using POS.Contract.Dtos.Common;
using POS.Contract.Dtos.SettingsDtos;
using POS.Core.Services.Contract.DataSyncServices;

namespace POS.API.Controllers;

public class DataSyncController : BaseApiController
{
    private readonly IDataSyncService _syncService;

    public DataSyncController(IDataSyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<HqSettingDto>> GetSettings()
    {
        return Ok(await _syncService.GetHqSettingsAsync());
    }

    [HttpPost("settings")]
    public async Task<ActionResult<BaseResponse>> UpdateSettings(HqSettingDto settings)
    {
        return Ok(await _syncService.UpdateHqSettingsAsync(settings));
    }

    [HttpPost("test-connection")]
    public async Task<ActionResult<BaseResponse>> TestConnection(HqSettingDto settings)
    {
        return Ok(await _syncService.TestHqConnectionAsync(settings));
    }

    [HttpPost("sync")]
    public async Task<ActionResult<BaseResponse>> SyncData(SyncRequestDto request)
    {
        return Ok(await _syncService.SyncDataFromHqAsync(request));
    }

    [HttpPost("databases")]
    public async Task<ActionResult<List<string>>> GetDatabases(HqSettingDto settings)
    {
        return Ok(await _syncService.GetDatabasesAsync(settings));
    }
}
