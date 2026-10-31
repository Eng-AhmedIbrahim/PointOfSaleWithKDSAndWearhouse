using Microsoft.AspNetCore.Mvc;
using POS.Contract.Dtos.OrderDto;
using POS.Core.Services.Contract.PosFeatureServices;

namespace POS.API.Controllers;

public class PosFeatureSettingsController : BaseApiController
{
    private readonly IPosFeatureSettingsService _featureSettingsService;

    public PosFeatureSettingsController(IPosFeatureSettingsService featureSettingsService)
    {
        _featureSettingsService = featureSettingsService;
    }

    [HttpGet("{computerName}")]
    public async Task<ActionResult<List<PosFeatureSettingToReturnDto>>> GetSettings(string computerName)
    {
        var settings = await _featureSettingsService.GetSettingsByComputerNameAsync(computerName);
        return Ok(settings);
    }

    [HttpPut("{computerName}")]
    public async Task<ActionResult> UpdateSettings(string computerName, [FromBody] List<PosFeatureSettingToReturnDto> settings)
    {
        var result = await _featureSettingsService.UpdateSettingsAsync(computerName, settings);
        if (result) return Ok();
        return BadRequest("Failed to update settings.");
    }
}
