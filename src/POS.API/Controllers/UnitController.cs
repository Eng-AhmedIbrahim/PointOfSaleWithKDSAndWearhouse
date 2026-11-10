using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Contract.Dtos.InventoryDtos;
using POS.Core.Entities.Item;
using POS.Core.Entities.Warehouse;
using POS.Core.Services.Contract.InventoryServices;
using Pos.Repository.Data;
using Unit = POS.Core.Entities.Item.Unit;

namespace POS.API.Controllers;

[Route("api/units")]
[Route("api/unit")]
public class UnitController : BaseApiController
{
    private readonly IUnitService _unitService;
    private readonly AppDbContext _db;

    public UnitController(IUnitService unitService, AppDbContext db)
    {
        _unitService = unitService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var units = await _unitService.GetAllUnitsAsync();
        var dtos = units.Select(u => new UnitDto
        {
            Id = u.Id,
            ArabicName = u.ArabicName,
            EnglishName = u.EnglishName,
            Code = u.Code
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var unit = await _unitService.GetUnitByIdAsync(id);
        if (unit == null) return NotFound();
        
        return Ok(new UnitDto
        {
            Id = unit.Id,
            ArabicName = unit.ArabicName,
            EnglishName = unit.EnglishName,
            Code = unit.Code
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UnitDto dto)
    {
        var arName = !string.IsNullOrWhiteSpace(dto.ArabicName) ? dto.ArabicName : (dto.EnglishName ?? "");
        var enName = !string.IsNullOrWhiteSpace(dto.EnglishName) ? dto.EnglishName : arName;
        var code = !string.IsNullOrWhiteSpace(dto.Code) ? dto.Code : arName;

        var unit = new Unit
        {
            ArabicName = arName,
            EnglishName = enName,
            Code = code,
            CreatedAt = DateTime.UtcNow
        };
        await _unitService.CreateUnitAsync(unit);

        // Also synchronize with WarehouseUnits so Warehouse picks it up immediately
        try
        {
            if (!await _db.WarehouseUnits.AnyAsync(w => w.NameAr == arName || w.Symbol == code))
            {
                _db.WarehouseUnits.Add(new WarehouseUnit
                {
                    NameAr = arName,
                    NameEn = enName,
                    Symbol = code,
                    IsBaseUnit = false
                });
                await _db.SaveChangesAsync();
            }
        }
        catch { }

        return Ok(true);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UnitDto dto)
    {
        var unit = await _unitService.GetUnitByIdAsync(dto.Id);
        if (unit == null) return NotFound();

        var oldArName = unit.ArabicName;
        unit.ArabicName = !string.IsNullOrWhiteSpace(dto.ArabicName) ? dto.ArabicName : unit.ArabicName;
        unit.EnglishName = !string.IsNullOrWhiteSpace(dto.EnglishName) ? dto.EnglishName : (!string.IsNullOrWhiteSpace(dto.ArabicName) ? dto.ArabicName : unit.EnglishName);
        unit.Code = !string.IsNullOrWhiteSpace(dto.Code) ? dto.Code : unit.Code;
        unit.UpdatedAt = DateTime.UtcNow;

        await _unitService.UpdateUnitAsync(unit);

        // Sync with WarehouseUnits
        try
        {
            var whUnit = await _db.WarehouseUnits.FirstOrDefaultAsync(w => w.NameAr == oldArName || w.Symbol == unit.Code);
            if (whUnit != null)
            {
                whUnit.NameAr = unit.ArabicName;
                whUnit.NameEn = unit.EnglishName;
                whUnit.Symbol = unit.Code;
                await _db.SaveChangesAsync();
            }
        }
        catch { }

        return Ok(true);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var unit = await _unitService.GetUnitByIdAsync(id);
        if (unit != null)
        {
            try
            {
                var whUnit = await _db.WarehouseUnits.FirstOrDefaultAsync(w => w.NameAr == unit.ArabicName || w.Symbol == unit.Code);
                if (whUnit != null)
                {
                    _db.WarehouseUnits.Remove(whUnit);
                    await _db.SaveChangesAsync();
                }
            }
            catch { }
        }
        await _unitService.DeleteUnitAsync(id);
        return Ok(true);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> GetLookup()
    {
        var units = await _unitService.GetAllUnitsAsync();
        return Ok(units.Select(u => new { u.Id, Name = u.ArabicName ?? u.EnglishName, NameAr = u.ArabicName, NameEn = u.EnglishName }));
    }
}
