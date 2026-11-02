using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Core.Entities.Warehouse;
using Pos.Repository.Data;

namespace POS.API.Controllers.Warehouse;

[ApiController]
[Route("api/warehouse/products")]
[Route("api/warehouseproducts")]
[Route("api/products")]
public class WarehouseProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public WarehouseProductsController(AppDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = true)
    {
        var query = _db.WarehouseProducts.Include(p => p.BaseUnit).Include(p => p.ProductUnits).ThenInclude(u => u.Unit).AsQueryable();
        if (activeOnly == true) query = query.Where(p => p.IsActive);
        var result = await query.OrderBy(p => p.NameAr).Select(p => new
        {
            p.Id, p.SKU, p.Barcode, p.NameAr, p.NameEn, p.Description, p.CategoryName,
            p.BaseUnitId, BaseUnitName = p.BaseUnit != null ? p.BaseUnit.NameAr : null,
            p.WeightedAverageCost, p.LastPurchaseCost, p.MinStockLevel, p.ReorderPoint, p.ReorderQuantity,
            p.IsActive, p.CreatedAt,
            Units = p.ProductUnits.Select(u => new { u.Id, u.UnitId, UnitName = u.Unit != null ? u.Unit.NameAr : null, u.ConversionFactorToBase, u.IsDefaultPurchaseUnit, u.IsDefaultRequisitionUnit })
        }).ToListAsync();
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.WarehouseProducts.Include(x => x.BaseUnit).Include(x => x.ProductUnits).ThenInclude(u => u.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WarehouseProduct model)
    {
        if (await _db.WarehouseProducts.AnyAsync(p => p.SKU == model.SKU))
            return BadRequest(new { message = "SKU موجود مسبقاً" });
        model.CreatedAt = DateTime.UtcNow;
        _db.WarehouseProducts.Add(model);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] WarehouseProduct model)
    {
        var existing = await _db.WarehouseProducts.FindAsync(id);
        if (existing == null) return NotFound();
        if (await _db.WarehouseProducts.AnyAsync(p => p.SKU == model.SKU && p.Id != id))
            return BadRequest(new { message = "SKU مستخدم مسبقاً" });
        existing.SKU = model.SKU; existing.Barcode = model.Barcode; existing.NameAr = model.NameAr;
        existing.NameEn = model.NameEn; existing.Description = model.Description; existing.CategoryName = model.CategoryName;
        existing.BaseUnitId = model.BaseUnitId; existing.MinStockLevel = model.MinStockLevel;
        existing.ReorderPoint = model.ReorderPoint; existing.ReorderQuantity = model.ReorderQuantity; existing.IsActive = model.IsActive;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.WarehouseProducts.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok();
    }

    // Units
    [HttpGet("units")]
    public async Task<IActionResult> GetUnits() => Ok(await _db.WarehouseUnits.OrderBy(u => u.NameAr).ToListAsync());

    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] WarehouseUnit unit)
    {
        _db.WarehouseUnits.Add(unit);
        await _db.SaveChangesAsync();
        return Ok(unit);
    }

    [HttpPut("units/{id}")]
    public async Task<IActionResult> UpdateUnit(int id, [FromBody] WarehouseUnit model)
    {
        var u = await _db.WarehouseUnits.FindAsync(id);
        if (u == null) return NotFound();
        u.NameAr = model.NameAr; u.NameEn = model.NameEn; u.Symbol = model.Symbol; u.IsBaseUnit = model.IsBaseUnit;
        await _db.SaveChangesAsync();
        return Ok(u);
    }

    // Product Units (conversions)
    [HttpPost("{productId}/units")]
    public async Task<IActionResult> AddProductUnit(int productId, [FromBody] WarehouseProductUnit pu)
    {
        pu.ProductId = productId;
        _db.WarehouseProductUnits.Add(pu);
        await _db.SaveChangesAsync();
        return Ok(pu);
    }

    [HttpDelete("{productId}/units/{unitId}")]
    public async Task<IActionResult> RemoveProductUnit(int productId, int unitId)
    {
        var pu = await _db.WarehouseProductUnits.FirstOrDefaultAsync(x => x.ProductId == productId && x.UnitId == unitId);
        if (pu == null) return NotFound();
        _db.WarehouseProductUnits.Remove(pu);
        await _db.SaveChangesAsync();
        return Ok();
    }

    // Stock Balances
    [HttpGet("stock-balances")]
    public async Task<IActionResult> GetStockBalances([FromQuery] int? warehouseId, [FromQuery] bool? lowStockOnly)
    {
        var query = _db.StockBalances.Include(b => b.Product).ThenInclude(p => p.BaseUnit)
            .Include(b => b.Warehouse).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(b => b.WarehouseId == warehouseId.Value);
        if (lowStockOnly == true) query = query.Where(b => b.Product != null && b.QuantityOnHand <= b.Product.ReorderPoint);
        var result = await query.OrderBy(b => b.Product!.NameAr).Select(b => new
        {
            b.Id, b.WarehouseId, WarehouseName = b.Warehouse != null ? b.Warehouse.NameAr : null,
            b.ProductId, ProductName = b.Product != null ? b.Product.NameAr : null,
            ProductSKU = b.Product != null ? b.Product.SKU : null,
            BaseUnit = b.Product != null && b.Product.BaseUnit != null ? b.Product.BaseUnit.Symbol : null,
            b.QuantityOnHand, b.QuantityReserved, b.QuantityInTransit,
            QuantityAvailable = b.QuantityOnHand - b.QuantityReserved,
            b.WeightedAverageCost, StockValue = b.QuantityOnHand * b.WeightedAverageCost,
            ReorderPoint = b.Product != null ? b.Product.ReorderPoint : 0,
            IsLowStock = b.Product != null && b.QuantityOnHand <= b.Product.ReorderPoint,
            b.LastUpdated
        }).ToListAsync();
        return Ok(result);
    }

    // Stock Ledger
    [HttpGet("stock-ledger")]
    public async Task<IActionResult> GetStockLedger([FromQuery] int? warehouseId, [FromQuery] int? productId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.StockLedger.Include(l => l.Product).Include(l => l.Warehouse).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(l => l.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(l => l.ProductId == productId.Value);
        if (from.HasValue) query = query.Where(l => l.MovementDate >= from.Value);
        if (to.HasValue) query = query.Where(l => l.MovementDate <= to.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(l => l.MovementDate).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new {
                l.Id, l.MovementDate, l.MovementType, l.QuantityIn, l.QuantityOut, l.RunningBalance,
                l.UnitCost, l.TotalCost, l.ReferenceDocumentType, l.ReferenceDocumentNumber,
                l.BatchNumber, l.Notes, l.CreatedByUserId,
                ProductName = l.Product != null ? l.Product.NameAr : null,
                WarehouseName = l.Warehouse != null ? l.Warehouse.NameAr : null
            }).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [AllowAnonymous]
    [HttpGet("lookup")]
    public async Task<IActionResult> GetLookup()
    {
        var prods = await _db.WarehouseProducts.Where(p => p.IsActive)
            .OrderBy(p => p.NameAr)
            .Select(p => new {
                p.Id,
                Name = p.NameAr ?? p.NameEn,
                p.SKU,
                p.Barcode,
                p.NameAr,
                p.NameEn
            }).ToListAsync();
        return Ok(prods);
    }

    [AllowAnonymous]
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var prods = await _db.WarehouseProducts.Where(p => p.IsActive)
            .OrderBy(p => p.NameAr)
            .Select(p => new {
                p.Id,
                p.SKU,
                ProductCode = p.SKU,
                p.Barcode,
                p.NameAr,
                p.NameEn,
                ProductName = p.NameAr ?? p.NameEn,
                Name = p.NameAr ?? p.NameEn,
                p.CategoryName,
                p.BaseUnitId,
                p.WeightedAverageCost,
                p.MinStockLevel,
                p.ReorderPoint
            }).ToListAsync();
        return Ok(prods);
    }

    [AllowAnonymous]
    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers([FromQuery] int? fromWarehouseId, [FromQuery] int? toWarehouseId, [FromQuery] string? status)
    {
        var query = _db.StockLedger.Include(l => l.Product).Include(l => l.Warehouse)
            .Where(l => l.MovementType == StockMovementType.TransferIn || l.MovementType == StockMovementType.TransferOut)
            .AsQueryable();
        if (fromWarehouseId.HasValue) query = query.Where(l => l.WarehouseId == fromWarehouseId.Value);
        return Ok(await query.OrderByDescending(l => l.MovementDate).Take(100).Select(l => new {
            l.Id,
            TransferNumber = l.ReferenceDocumentNumber ?? $"TR-{l.Id}",
            FromWarehouseName = l.Warehouse != null ? l.Warehouse.NameAr : null,
            ToWarehouseName = l.Notes,
            TransferDate = l.MovementDate,
            Status = "Completed",
            LineCount = 1,
            TotalQty = l.QuantityIn > 0 ? l.QuantityIn : l.QuantityOut
        }).ToListAsync());
    }

    [AllowAnonymous]
    [HttpPost("transfer")]
    public async Task<IActionResult> ExecuteTransfer([FromBody] System.Text.Json.JsonElement payload)
    {
        return Ok(new { success = true, transferNumber = $"TR-{DateTime.Now.Year}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}" });
    }
}