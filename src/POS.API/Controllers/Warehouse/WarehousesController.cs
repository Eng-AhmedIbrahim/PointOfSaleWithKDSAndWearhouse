using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Core.Entities.Warehouse;
using Pos.Repository.Data;

namespace POS.API.Controllers.Warehouse;

[ApiController]
[Route("api/warehouse/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly AppDbContext _db;
    public WarehousesController(AppDbContext db) => _db = db;

    // GET: مفتوح للجميع — يُستخدم في قوائم الاختيار بالفروع وبدون توثيق
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = true)
    {
        var query = _db.Warehouses.Include(w => w.Branch).Include(w => w.Locations).AsQueryable();
        if (activeOnly == true) query = query.Where(w => w.IsActive);
        var result = await query.OrderBy(w => w.Type).ThenBy(w => w.NameAr).Select(w => new
        {
            w.Id, w.Code, w.NameAr, w.NameEn, w.Type, w.Address, w.Phone,
            w.ManagerName, w.IsActive, w.BranchId, BranchName = w.Branch != null ? w.Branch.Name : null,
            w.DefaultSupplyingWarehouseId, w.CreatedAt, w.ApiUrl,
            LocationCount = w.Locations.Count
        }).ToListAsync();
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var w = await _db.Warehouses.Include(x => x.Locations).Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (w == null) return NotFound();
        return Ok(w);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] POS.Core.Entities.Warehouse.Warehouse model)
    {
        if (await _db.Warehouses.AnyAsync(w => w.Code == model.Code))
            return BadRequest(new { message = "كود المستودع مستخدم بالفعل" });
        model.CreatedAt = DateTime.UtcNow;
        _db.Warehouses.Add(model);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] POS.Core.Entities.Warehouse.Warehouse model)
    {
        var existing = await _db.Warehouses.FindAsync(id);
        if (existing == null) return NotFound();
        if (await _db.Warehouses.AnyAsync(w => w.Code == model.Code && w.Id != id))
            return BadRequest(new { message = "الكود مستخدم لمستودع آخر" });
        existing.Code = model.Code; existing.NameAr = model.NameAr; existing.NameEn = model.NameEn;
        existing.Type = model.Type; existing.Address = model.Address; existing.Phone = model.Phone;
        existing.ManagerName = model.ManagerName; existing.IsActive = model.IsActive;
        existing.BranchId = model.BranchId; existing.DefaultSupplyingWarehouseId = model.DefaultSupplyingWarehouseId;
        existing.ApiUrl = model.ApiUrl;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var w = await _db.Warehouses.FindAsync(id);
        if (w == null) return NotFound();
        w.IsActive = false; w.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "تم إيقاف تشغيل المستودع" });
    }

    // Locations sub-resource
    [AllowAnonymous]
    [HttpGet("{warehouseId}/locations")]
    public async Task<IActionResult> GetLocations(int warehouseId)
    {
        var locs = await _db.WarehouseLocations.Where(l => l.WarehouseId == warehouseId && l.IsActive)
            .OrderBy(l => l.Zone).ThenBy(l => l.Shelf).ToListAsync();
        return Ok(locs);
    }

    [AllowAnonymous]
    [HttpPost("{warehouseId}/locations")]
    public async Task<IActionResult> AddLocation(int warehouseId, [FromBody] WarehouseLocation loc)
    {
        try
        {
            loc.WarehouseId = warehouseId;
            if (string.IsNullOrWhiteSpace(loc.NameEn))
            {
                loc.NameEn = loc.NameAr ?? loc.Code;
            }
            _db.WarehouseLocations.Add(loc);
            await _db.SaveChangesAsync();
            return Ok(loc);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPut("locations/{id}")]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] WarehouseLocation model)
    {
        var loc = await _db.WarehouseLocations.FindAsync(id);
        if (loc == null) return NotFound();
        loc.Code = model.Code; loc.NameAr = model.NameAr; loc.NameEn = string.IsNullOrWhiteSpace(model.NameEn) ? model.NameAr : model.NameEn;
        loc.Zone = model.Zone; loc.Shelf = model.Shelf; loc.Bin = model.Bin; loc.IsActive = model.IsActive;
        await _db.SaveChangesAsync();
        return Ok(loc);
    }

    [AllowAnonymous]
    [HttpDelete("locations/{id}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        var loc = await _db.WarehouseLocations.FindAsync(id);
        if (loc == null) return NotFound();
        _db.WarehouseLocations.Remove(loc);
        await _db.SaveChangesAsync();
        return Ok();
    }
}