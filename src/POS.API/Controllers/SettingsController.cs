/* file: POS.API/Controllers/SettingsController.cs */
using Microsoft.AspNetCore.Mvc;
using POS.Contract.Dtos.SettingsDtos;
using POS.Core.Entities.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace POS.API.Controllers;

public class SettingsController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public SettingsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("dispatcher/{computerName}")]
    public async Task<ActionResult<DispatcherSettingsDto>> GetDispatcherSettings(string computerName)
    {
        var settings = (await _unitOfWork.Repository<DispatcherSetting>().GetAllAsync())
            .FirstOrDefault(s => s.ComputerName == computerName);
            
        if (settings == null)
        {
            return Ok(new DispatcherSettingsDto { ComputerName = computerName });
        }

        return Ok(new DispatcherSettingsDto
        {
            Id = settings.Id,
            RefreshTimeForDeliveryOrderColorsPerSecond = settings.RefreshTimeForDeliveryOrderColorsPerSecond,
            CriticalTimeForDeliveryOrderPerMinute = settings.CriticalTimeForDeliveryOrderPerMinute,
            WarningTimeForDeliveryOrderPerMinute = settings.WarningTimeForDeliveryOrderPerMinute,
            VoidLimitMinutesForDeliveryOrder = settings.VoidLimitMinutesForDeliveryOrder,
            IsDispatcher = settings.IsDispatcher,
            AllowVoidLimitMinutesForDeliveryOrder = settings.AllowVoidLimitMinutesForDeliveryOrder,
            AllowDeliveryVoidFromBranch = settings.AllowDeliveryVoidFromBranch,
            ComputerName = settings.ComputerName
        });
    }

    [HttpPost("dispatcher")]
    public async Task<ActionResult<DispatcherSettingsDto>> UpdateDispatcherSettings(DispatcherSettingsDto dto)
    {
        if (string.IsNullOrEmpty(dto.ComputerName)) return BadRequest("Computer name is required");

        var settings = (await _unitOfWork.Repository<DispatcherSetting>().GetAllAsync())
            .FirstOrDefault(s => s.ComputerName == dto.ComputerName);
        
        bool isNew = false;
        if (settings == null)
        {
            settings = new DispatcherSetting { ComputerName = dto.ComputerName };
            isNew = true;
        }

        settings.RefreshTimeForDeliveryOrderColorsPerSecond = dto.RefreshTimeForDeliveryOrderColorsPerSecond;
        settings.CriticalTimeForDeliveryOrderPerMinute = dto.CriticalTimeForDeliveryOrderPerMinute;
        settings.WarningTimeForDeliveryOrderPerMinute = dto.WarningTimeForDeliveryOrderPerMinute;
        settings.VoidLimitMinutesForDeliveryOrder = dto.VoidLimitMinutesForDeliveryOrder;
        settings.IsDispatcher = dto.IsDispatcher;
        settings.AllowVoidLimitMinutesForDeliveryOrder = dto.AllowVoidLimitMinutesForDeliveryOrder;
        settings.AllowDeliveryVoidFromBranch = dto.AllowDeliveryVoidFromBranch;

        if (isNew)
            await _unitOfWork.Repository<DispatcherSetting>().AddAsync(settings);
        else
            _unitOfWork.Repository<DispatcherSetting>().Update(settings);

        await _unitOfWork.CompleteAsync();

        dto.Id = settings.Id;
        return Ok(dto);
    }
}
