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
        var query = _db.WarehouseProducts.Include(p => p.BaseUnit).Include(p => p.Category).Include(p => p.ProductUnits).ThenInclude(u => u.Unit).AsQueryable();
        if (activeOnly == true) query = query.Where(p => p.IsActive);
        var result = await query.OrderBy(p => p.NameAr).Select(p => new
        {
            p.Id, p.SKU, p.Barcode, p.NameAr, p.NameEn, p.Description,
            p.CategoryId,
            CategoryName = p.Category != null ? p.Category.NameAr : p.CategoryName,
            CategoryCode = p.Category != null ? p.Category.Code : null,
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
        var p = await _db.WarehouseProducts
            .Include(x => x.BaseUnit)
            .Include(x => x.Category)
            .Include(x => x.ProductUnits).ThenInclude(u => u.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        return Ok(new {
            p.Id, p.SKU, p.Barcode, p.NameAr, p.NameEn, p.Description,
            p.CategoryId,
            CategoryName = p.Category != null ? p.Category.NameAr : p.CategoryName,
            CategoryCode = p.Category != null ? p.Category.Code : null,
            p.BaseUnitId, BaseUnitName = p.BaseUnit != null ? p.BaseUnit.NameAr : null,
            p.WeightedAverageCost, p.LastPurchaseCost, p.MinStockLevel,
            p.ReorderPoint, p.ReorderQuantity, p.IsActive, p.CreatedAt,
            Units = p.ProductUnits.Select(u => new {
                u.Id, u.UnitId,
                UnitName = u.Unit != null ? u.Unit.NameAr : null,
                UnitSymbol = u.Unit != null ? u.Unit.Symbol : null,
                u.ConversionFactorToBase,
                u.IsDefaultPurchaseUnit,
                u.IsDefaultRequisitionUnit
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WarehouseProduct model)
    {
        if (await _db.WarehouseProducts.AnyAsync(p => p.SKU == model.SKU))
            return BadRequest(new { message = "SKU موجود مسبقاً" });
        if (model.CategoryId.HasValue)
        {
            var cat = await _db.WarehouseCategories.FindAsync(model.CategoryId.Value);
            if (cat != null) model.CategoryName = cat.NameAr;
        }
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
        existing.NameEn = model.NameEn; existing.Description = model.Description;
        existing.CategoryId = model.CategoryId;
        if (model.CategoryId.HasValue)
        {
            var cat = await _db.WarehouseCategories.FindAsync(model.CategoryId.Value);
            if (cat != null) existing.CategoryName = cat.NameAr;
            else existing.CategoryName = model.CategoryName;
        }
        else
        {
            existing.CategoryName = model.CategoryName;
        }
        existing.BaseUnitId = model.BaseUnitId; existing.MinStockLevel = model.MinStockLevel;
        existing.ReorderPoint = model.ReorderPoint; existing.ReorderQuantity = model.ReorderQuantity; existing.IsActive = model.IsActive;
        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    // Warehouse Categories
    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _db.WarehouseCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.NameAr,
                c.NameEn,
                c.Description,
                c.IsActive,
                ProductCount = c.Products.Count(p => p.IsActive)
            }).ToListAsync();
        return Ok(cats);
    }

    [AllowAnonymous]
    [HttpGet("categories/{id}")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var cat = await _db.WarehouseCategories.FindAsync(id);
        if (cat == null) return NotFound();
        return Ok(cat);
    }

    [AllowAnonymous]
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] WarehouseCategory model)
    {
        if (model.Id <= 0)
        {
            var maxId = await _db.WarehouseCategories.MaxAsync(c => (int?)c.Id) ?? 100;
            model.Id = maxId + 1;
        }

        if (await _db.WarehouseCategories.AnyAsync(c => c.Id == model.Id))
            return BadRequest(new { message = $"الفئة ذات الرقم المعرف {model.Id} موجودة مسبقاً" });

        _db.WarehouseCategories.Add(model);
        await _db.SaveChangesAsync();
        return Ok(model);
    }

    [AllowAnonymous]
    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] WarehouseCategory model)
    {
        var cat = await _db.WarehouseCategories.FindAsync(id);
        if (cat == null) return NotFound();

        cat.Code = model.Code;
        cat.NameAr = model.NameAr;
        cat.NameEn = model.NameEn;
        cat.Description = model.Description;
        cat.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        return Ok(cat);
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
    [AllowAnonymous]
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
            ProductCode = b.Product != null ? b.Product.SKU : null,
            BaseUnit = b.Product != null && b.Product.BaseUnit != null ? (b.Product.BaseUnit.NameAr ?? b.Product.BaseUnit.Symbol) : null,
            UnitName = b.Product != null && b.Product.BaseUnit != null ? (b.Product.BaseUnit.NameAr ?? b.Product.BaseUnit.Symbol) : null,
            b.QuantityOnHand, OnHandQty = b.QuantityOnHand,
            b.QuantityReserved, ReservedQty = b.QuantityReserved,
            b.QuantityInTransit,
            QuantityAvailable = b.QuantityOnHand - b.QuantityReserved,
            b.WeightedAverageCost, StockValue = b.QuantityOnHand * b.WeightedAverageCost,
            ReorderPoint = b.Product != null ? b.Product.ReorderPoint : 0,
            ReorderLevel = b.Product != null ? b.Product.ReorderPoint : 0,
            IsLowStock = b.Product != null && b.QuantityOnHand <= b.Product.ReorderPoint,
            b.LastUpdated
        }).ToListAsync();
        return Ok(result);
    }

    // Stock Ledger
    [AllowAnonymous]
    [HttpGet("stock-ledger")]
    public async Task<IActionResult> GetStockLedger([FromQuery] int? warehouseId, [FromQuery] int? productId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var query = _db.StockLedger.Include(l => l.Product).ThenInclude(p => p!.BaseUnit).Include(l => l.Warehouse).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(l => l.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(l => l.ProductId == productId.Value);
        if (from.HasValue) query = query.Where(l => l.MovementDate >= from.Value);
        if (to.HasValue) query = query.Where(l => l.MovementDate <= to.Value);
        var items = await query.OrderByDescending(l => l.MovementDate).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new {
                l.Id,
                Date = l.MovementDate,
                l.MovementDate,
                TransactionRef = !string.IsNullOrEmpty(l.ReferenceDocumentNumber) ? l.ReferenceDocumentNumber : $"MOV-{l.Id:D5}",
                TransactionType = l.MovementType.ToString(),
                l.MovementType,
                ProductName = l.Product != null ? (l.Product.NameAr ?? l.Product.NameEn) : null,
                WarehouseName = l.Warehouse != null ? l.Warehouse.NameAr : null,
                Qty = l.QuantityIn > 0 ? l.QuantityIn : -l.QuantityOut,
                l.QuantityIn,
                l.QuantityOut,
                l.RunningBalance,
                UnitName = (l.Product != null && l.Product.BaseUnit != null) ? l.Product.BaseUnit.NameAr : "",
                CostPrice = l.UnitCost,
                l.UnitCost,
                l.TotalCost,
                l.Notes,
                CreatedBy = l.CreatedByUserId ?? "النظام",
                l.CreatedByUserId
            }).ToListAsync();
        return Ok(items);
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

    [AllowAnonymous]
    [HttpGet("adjustments")]
    public async Task<IActionResult> GetAdjustments([FromQuery] int? warehouseId)
    {
        var query = _db.StockLedger.Include(l => l.Product).Include(l => l.Warehouse)
            .Where(l => l.MovementType == StockMovementType.InventoryAdjustment || l.MovementType == StockMovementType.WasteDamage)
            .AsQueryable();
        if (warehouseId.HasValue) query = query.Where(l => l.WarehouseId == warehouseId.Value);
        var list = await query.OrderByDescending(l => l.MovementDate).Take(100).Select(l => new
        {
            l.Id,
            Reference = !string.IsNullOrEmpty(l.ReferenceDocumentNumber) ? l.ReferenceDocumentNumber : $"ADJ-{l.Id:D5}",
            Date = l.MovementDate,
            WarehouseName = l.Warehouse != null ? l.Warehouse.NameAr : "مستودع",
            ProductName = l.Product != null ? (l.Product.NameAr ?? l.Product.NameEn) : "صنف",
            l.MovementType,
            TypeLabel = l.MovementType == StockMovementType.WasteDamage ? "هالك / تالف" : (l.QuantityIn > 0 ? "تسوية زيادة (+)" : "تسوية عجز (-)"),
            Qty = l.QuantityIn > 0 ? l.QuantityIn : -l.QuantityOut,
            l.RunningBalance,
            l.UnitCost,
            l.TotalCost,
            l.Notes,
            CreatedBy = l.CreatedByUserId ?? "أمين المخزن"
        }).ToListAsync();
        return Ok(list);
    }

    public class StockAdjustmentRequest
    {
        public int WarehouseId { get; set; }
        public int ProductId { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal CurrentQuantity { get; set; }
        public string Reason { get; set; } = "تسوية جرد دوري";
        public bool IsDamage { get; set; }
        public string? Notes { get; set; }
    }

    [AllowAnonymous]
    [HttpPost("adjust")]
    public async Task<IActionResult> PostAdjustment([FromBody] StockAdjustmentRequest req)
    {
        var balance = await _db.StockBalances.FirstOrDefaultAsync(b => b.WarehouseId == req.WarehouseId && b.ProductId == req.ProductId);
        if (balance == null)
        {
            balance = new StockBalance { WarehouseId = req.WarehouseId, ProductId = req.ProductId, QuantityOnHand = 0 };
            _db.StockBalances.Add(balance);
        }

        var diff = req.ActualQuantity - balance.QuantityOnHand;
        if (diff == 0) return BadRequest(new { message = "الكمية الفعلية مطابقة للرصيد الدفتري، لا يوجد فرق تسوية" });

        var product = await _db.WarehouseProducts.FindAsync(req.ProductId);
        var cost = product?.WeightedAverageCost ?? 0m;
        var now = DateTime.UtcNow;
        var movementType = req.IsDamage ? StockMovementType.WasteDamage : StockMovementType.InventoryAdjustment;

        balance.QuantityOnHand = req.ActualQuantity;
        balance.LastUpdated = now;

        var count = await _db.StockLedger.CountAsync(l => l.MovementType == StockMovementType.InventoryAdjustment || l.MovementType == StockMovementType.WasteDamage) + 1;
        var refDoc = $"ADJ-{now.Year}-{count:D4}";

        var ledger = new StockLedger
        {
            WarehouseId = req.WarehouseId,
            ProductId = req.ProductId,
            MovementType = movementType,
            QuantityIn = diff > 0 ? diff : 0,
            QuantityOut = diff < 0 ? -diff : 0,
            RunningBalance = balance.QuantityOnHand,
            UnitCost = cost,
            TotalCost = Math.Abs(diff) * cost,
            ReferenceDocumentType = req.IsDamage ? "تالف" : "تسوية جرد",
            ReferenceDocumentNumber = refDoc,
            Notes = $"{req.Reason}: {req.Notes}".TrimEnd(':', ' '),
            MovementDate = now,
            CreatedByUserId = "أمين المخزن"
        };
        _db.StockLedger.Add(ledger);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, reference = refDoc, newQuantity = balance.QuantityOnHand });
    }
}
