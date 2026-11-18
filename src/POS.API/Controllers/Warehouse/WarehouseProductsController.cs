using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<AppUser> _userManager;
    public WarehouseProductsController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly = true)
    {
        var query = _db.WarehouseProducts
            .Include(p => p.BaseUnit)
            .Include(p => p.Category)
            .Include(p => p.ProductUnits).ThenInclude(u => u.Unit)
            .Include(p => p.Balances)
            .AsQueryable();

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
            CurrentStock = p.Balances.Sum(b => (decimal?)b.QuantityOnHand) ?? 0m,
            CostPrice = p.WeightedAverageCost > 0 ? p.WeightedAverageCost : p.LastPurchaseCost,
            ReorderLevel = p.ReorderPoint,
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
    public async Task<IActionResult> Update(int id, [FromBody] WarehouseProductUpdateDto dto)
    {
        var existing = await _db.WarehouseProducts
            .Include(p => p.ProductUnits)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (existing == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(dto.SKU) && dto.SKU != existing.SKU)
        {
            if (await _db.WarehouseProducts.AnyAsync(p => p.SKU == dto.SKU && p.Id != id))
                return BadRequest(new { message = "SKU مستخدم مسبقاً" });
            existing.SKU = dto.SKU;
            existing.Barcode = dto.Barcode ?? dto.SKU;
        }
        else if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            existing.Barcode = dto.Barcode;
        }

        if (!string.IsNullOrWhiteSpace(dto.NameAr)) existing.NameAr = dto.NameAr;
        if (dto.NameEn != null) existing.NameEn = dto.NameEn;
        if (dto.Description != null) existing.Description = dto.Description;
        existing.CategoryId = dto.CategoryId;
        if (dto.CategoryId.HasValue)
        {
            var cat = await _db.WarehouseCategories.FindAsync(dto.CategoryId.Value);
            existing.CategoryName = cat?.NameAr ?? dto.CategoryName;
        }
        else
        {
            existing.CategoryName = dto.CategoryName;
        }
        if (dto.BaseUnitId > 0) existing.BaseUnitId = dto.BaseUnitId;
        if (dto.MinStockLevel.HasValue) existing.MinStockLevel = dto.MinStockLevel.Value;
        if (dto.ReorderPoint.HasValue) existing.ReorderPoint = dto.ReorderPoint.Value;
        if (dto.ReorderQuantity.HasValue) existing.ReorderQuantity = dto.ReorderQuantity.Value;
        existing.IsActive = dto.IsActive;
        if (dto.WeightedAverageCost.HasValue && dto.WeightedAverageCost.Value > 0)
            existing.WeightedAverageCost = dto.WeightedAverageCost.Value;
        if (dto.LastPurchaseCost.HasValue && dto.LastPurchaseCost.Value > 0)
            existing.LastPurchaseCost = dto.LastPurchaseCost.Value;

        // ===== Sync ProductUnits =====
        if (dto.ProductUnits != null)
        {
            var incomingIds = dto.ProductUnits.Where(u => u.Id > 0).Select(u => u.Id).ToHashSet();
            var toRemove = existing.ProductUnits.Where(u => !incomingIds.Contains(u.Id)).ToList();
            _db.WarehouseProductUnits.RemoveRange(toRemove);

            foreach (var unitDto in dto.ProductUnits.Where(u => u.UnitId > 0))
            {
                if (unitDto.Id > 0)
                {
                    var existingUnit = existing.ProductUnits.FirstOrDefault(u => u.Id == unitDto.Id);
                    if (existingUnit != null)
                    {
                        existingUnit.UnitId = unitDto.UnitId;
                        existingUnit.ConversionFactorToBase = unitDto.ConversionFactorToBase > 0 ? unitDto.ConversionFactorToBase : 1;
                        existingUnit.IsDefaultPurchaseUnit = unitDto.IsDefaultPurchaseUnit;
                        existingUnit.IsDefaultRequisitionUnit = unitDto.IsDefaultRequisitionUnit;
                    }
                }
                else
                {
                    existing.ProductUnits.Add(new WarehouseProductUnit
                    {
                        ProductId = id,
                        UnitId = unitDto.UnitId,
                        ConversionFactorToBase = unitDto.ConversionFactorToBase > 0 ? unitDto.ConversionFactorToBase : 1,
                        IsDefaultPurchaseUnit = unitDto.IsDefaultPurchaseUnit,
                        IsDefaultRequisitionUnit = unitDto.IsDefaultRequisitionUnit
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { existing.Id, existing.SKU, existing.NameAr, message = "تم التعديل بنجاح" });
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

    [AllowAnonymous]
    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var cat = await _db.WarehouseCategories.FindAsync(id);
        if (cat == null) return NotFound();

        bool hasProducts = await _db.WarehouseProducts.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
            return BadRequest(new { message = "لا يمكن حذف الفئة لأنها مرتبطة بمنتجات مخزنية بالفعل" });

        _db.WarehouseCategories.Remove(cat);
        await _db.SaveChangesAsync();
        return Ok(new { message = "تم حذف الفئة بنجاح" });
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
    public async Task<IActionResult> GetUnits()
    {
        // Auto-sync any Units that might be missing in WarehouseUnits
        try
        {
            var whUnits = await _db.WarehouseUnits.ToListAsync();
            var branchUnits = await _db.Units.ToListAsync();
            bool changed = false;
            foreach (var bu in branchUnits)
            {
                if (!whUnits.Any(w => w.NameAr == bu.ArabicName || (!string.IsNullOrWhiteSpace(bu.Code) && w.Symbol == bu.Code)))
                {
                    var newWh = new WarehouseUnit
                    {
                        NameAr = bu.ArabicName ?? bu.EnglishName ?? "",
                        NameEn = bu.EnglishName ?? bu.ArabicName ?? "",
                        Symbol = bu.Code ?? "",
                        IsBaseUnit = false
                    };
                    _db.WarehouseUnits.Add(newWh);
                    changed = true;
                }
            }
            if (changed)
            {
                await _db.SaveChangesAsync();
            }
        }
        catch { }

        var list = await _db.WarehouseUnits.OrderBy(u => u.NameAr).Select(u => new
        {
            u.Id,
            ArabicName = u.NameAr,
            EnglishName = u.NameEn,
            Name = u.NameAr,
            NameAr = u.NameAr,
            NameEn = u.NameEn,
            Code = u.Symbol,
            Symbol = u.Symbol,
            u.IsBaseUnit,
            DisplayName = !string.IsNullOrWhiteSpace(u.NameAr) ? u.NameAr : u.NameEn
        }).ToListAsync();
        return Ok(list);
    }

    [AllowAnonymous]
    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] WarehouseUnit unit)
    {
        _db.WarehouseUnits.Add(unit);
        try
        {
            if (!await _db.Units.AnyAsync(u => u.ArabicName == unit.NameAr || u.Code == unit.Symbol))
            {
                _db.Units.Add(new POS.Core.Entities.Item.Unit
                {
                    ArabicName = unit.NameAr,
                    EnglishName = string.IsNullOrWhiteSpace(unit.NameEn) ? unit.NameAr : unit.NameEn,
                    Code = string.IsNullOrWhiteSpace(unit.Symbol) ? unit.NameAr : unit.Symbol,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        catch { }
        await _db.SaveChangesAsync();
        return Ok(unit);
    }

    [AllowAnonymous]
    [HttpPut("units/{id}")]
    public async Task<IActionResult> UpdateUnit(int id, [FromBody] WarehouseUnit model)
    {
        var u = await _db.WarehouseUnits.FindAsync(id);
        if (u == null) return NotFound();
        u.NameAr = model.NameAr; u.NameEn = model.NameEn; u.Symbol = model.Symbol; u.IsBaseUnit = model.IsBaseUnit;
        await _db.SaveChangesAsync();
        return Ok(u);
    }

    [AllowAnonymous]
    [HttpDelete("units/{id}")]
    public async Task<IActionResult> DeleteUnit(int id)
    {
        var u = await _db.WarehouseUnits.FindAsync(id);
        if (u == null) return NotFound();

        bool hasProducts = await _db.WarehouseProducts.AnyAsync(p => p.BaseUnitId == id) ||
                           await _db.WarehouseProductUnits.AnyAsync(pu => pu.UnitId == id);
        if (hasProducts)
            return BadRequest(new { message = "لا يمكن حذف الوحدة لأنها مرتبطة بمنتجات مخزنية بالفعل" });

        _db.WarehouseUnits.Remove(u);
        await _db.SaveChangesAsync();
        return Ok(new { message = "تم حذف الوحدة بنجاح" });
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
        var rawItems = await query.OrderByDescending(l => l.MovementDate).Skip((page - 1) * pageSize).Take(pageSize)
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
                l.CreatedByUserId
            }).ToListAsync();

        // جلب أسماء المستخدمين
        var userIds = rawItems.Where(i => !string.IsNullOrEmpty(i.CreatedByUserId)).Select(i => i.CreatedByUserId!).Distinct().ToList();
        var users = _userManager.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, DisplayName = u.DisplayName ?? u.UserName ?? u.Email }).ToDictionary(u => u.Id, u => u.DisplayName);

        var items = rawItems.Select(l => new {
            l.Id, l.Date, l.MovementDate, l.TransactionRef, l.TransactionType, l.MovementType,
            l.ProductName, l.WarehouseName, l.Qty, l.QuantityIn, l.QuantityOut, l.RunningBalance,
            l.UnitName, l.CostPrice, l.UnitCost, l.TotalCost, l.Notes, l.CreatedByUserId,
            CreatedBy = string.IsNullOrEmpty(l.CreatedByUserId) ? "النظام" : (users.TryGetValue(l.CreatedByUserId, out var name) ? name : l.CreatedByUserId)
        }).ToList();

        return Ok(items);
    }

    [AllowAnonymous]
    [HttpGet("lookup")]
    public async Task<IActionResult> GetLookup()
    {
        var prods = await _db.WarehouseProducts.Where(p => p.IsActive)
            .Include(p => p.BaseUnit)
            .OrderBy(p => p.NameAr)
            .Select(p => new {
                p.Id,
                Name = p.NameAr ?? p.NameEn,
                p.SKU,
                p.Barcode,
                p.NameAr,
                p.NameEn,
                BaseUnitName = p.BaseUnit != null ? p.BaseUnit.NameAr : null,
                UnitName = p.BaseUnit != null ? p.BaseUnit.NameAr : null,
                p.WeightedAverageCost,
                p.LastPurchaseCost
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
    public async Task<IActionResult> ExecuteTransfer([FromBody] StockTransferRequestDto dto)
    {
        if (dto == null || dto.FromWarehouseId <= 0 || dto.ToWarehouseId <= 0)
            return BadRequest(new { message = "يرجى تحديد المخزن المصدر والمخزن المستهدف" });

        if (dto.FromWarehouseId == dto.ToWarehouseId)
            return BadRequest(new { message = "يجب أن يختلف المخزن المصدر عن المخزن المستهدف" });

        if (dto.Lines == null || !dto.Lines.Any(l => l.ProductId > 0 && l.Qty > 0))
            return BadRequest(new { message = "يرجى إضافة صنف واحد على الأقل بكمية صحيحة" });

        var fromWarehouse = await _db.Warehouses.FindAsync(dto.FromWarehouseId);
        var toWarehouse = await _db.Warehouses.FindAsync(dto.ToWarehouseId);
        if (fromWarehouse == null || toWarehouse == null)
            return BadRequest(new { message = "أحد المستودعات المحددة غير موجود" });

        var year = DateTime.Now.Year;
        var transferNumber = $"TR-{year}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var movementDate = DateTime.TryParse(dto.TransferDate, out var td) ? td : DateTime.UtcNow;

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var line in dto.Lines.Where(l => l.ProductId > 0 && l.Qty > 0))
            {
                var product = await _db.WarehouseProducts
                    .Include(p => p.ProductUnits)
                    .FirstOrDefaultAsync(p => p.Id == line.ProductId);

                if (product == null) continue;

                // Unit conversion to base unit
                decimal baseQty = line.Qty;
                if (line.UnitId.HasValue && line.UnitId.Value != product.BaseUnitId)
                {
                    var pu = product.ProductUnits?.FirstOrDefault(u => u.UnitId == line.UnitId.Value);
                    if (pu != null && pu.ConversionFactorToBase > 0)
                        baseQty *= pu.ConversionFactorToBase;
                }

                // Check source warehouse stock
                var fromBalance = await _db.StockBalances
                    .FirstOrDefaultAsync(b => b.WarehouseId == dto.FromWarehouseId && b.ProductId == line.ProductId);

                decimal available = fromBalance?.QuantityOnHand ?? 0m;
                if (available < baseQty)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { message = $"رصيد الصنف '{product.NameAr}' في مستودع '{fromWarehouse.NameAr}' غير كافٍ. المتاح: {available:N2}, المطلوب: {baseQty:N2}" });
                }

                // 1. Deduct from source warehouse
                fromBalance!.QuantityOnHand -= baseQty;
                fromBalance.LastUpdated = DateTime.UtcNow;

                decimal unitCost = product.WeightedAverageCost > 0 ? product.WeightedAverageCost : product.LastPurchaseCost;

                _db.StockLedger.Add(new StockLedger
                {
                    WarehouseId = dto.FromWarehouseId,
                    ProductId = line.ProductId,
                    MovementType = StockMovementType.TransferOut,
                    QuantityIn = 0,
                    QuantityOut = baseQty,
                    RunningBalance = fromBalance.QuantityOnHand,
                    UnitCost = unitCost,
                    TotalCost = Math.Round(baseQty * unitCost, 2),
                    ReferenceDocumentType = "StockTransfer",
                    ReferenceDocumentNumber = transferNumber,
                    MovementDate = movementDate,
                    Notes = $"تحويل إلى {toWarehouse.NameAr} | {line.Notes ?? dto.Notes}"
                });

                // 2. Add to destination warehouse
                var toBalance = await _db.StockBalances
                    .FirstOrDefaultAsync(b => b.WarehouseId == dto.ToWarehouseId && b.ProductId == line.ProductId);

                if (toBalance == null)
                {
                    toBalance = new StockBalance
                    {
                        WarehouseId = dto.ToWarehouseId,
                        ProductId = line.ProductId,
                        QuantityOnHand = baseQty,
                        LastUpdated = DateTime.UtcNow
                    };
                    _db.StockBalances.Add(toBalance);
                }
                else
                {
                    toBalance.QuantityOnHand += baseQty;
                    toBalance.LastUpdated = DateTime.UtcNow;
                }

                _db.StockLedger.Add(new StockLedger
                {
                    WarehouseId = dto.ToWarehouseId,
                    ProductId = line.ProductId,
                    MovementType = StockMovementType.TransferIn,
                    QuantityIn = baseQty,
                    QuantityOut = 0,
                    RunningBalance = toBalance.QuantityOnHand,
                    UnitCost = unitCost,
                    TotalCost = Math.Round(baseQty * unitCost, 2),
                    ReferenceDocumentType = "StockTransfer",
                    ReferenceDocumentNumber = transferNumber,
                    MovementDate = movementDate,
                    Notes = $"تحويل وارد من {fromWarehouse.NameAr} | {line.Notes ?? dto.Notes}"
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { success = true, transferNumber, message = "تم تنفيذ التحويل بين المستودعات وتحديث الأرصدة بنجاح" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { message = $"فشل تنفيذ التحويل: {ex.Message}" });
        }
    }
public class StockTransferRequestDto
{
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public string? TransferDate { get; set; }
    public string? Notes { get; set; }
    public List<StockTransferLineRequestDto> Lines { get; set; } = new();
}

public class StockTransferLineRequestDto
{
    public int ProductId { get; set; }
    public int? UnitId { get; set; }
    public decimal Qty { get; set; }
    public int? FromLocationId { get; set; }
    public int? ToLocationId { get; set; }
    public string? Notes { get; set; }
}


    [AllowAnonymous]
    [HttpGet("adjustments")]
    public async Task<IActionResult> GetAdjustments([FromQuery] int? warehouseId)
    {
        var query = _db.StockLedger.Include(l => l.Product).Include(l => l.Warehouse)
            .Where(l => l.MovementType == StockMovementType.InventoryAdjustment || l.MovementType == StockMovementType.WasteDamage)
            .AsQueryable();
        if (warehouseId.HasValue) query = query.Where(l => l.WarehouseId == warehouseId.Value);

        var rawList = await query.OrderByDescending(l => l.MovementDate).Take(100).Select(l => new
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
            l.CreatedByUserId
        }).ToListAsync();

        var userIds = rawList.Where(i => !string.IsNullOrEmpty(i.CreatedByUserId)).Select(i => i.CreatedByUserId!).Distinct().ToList();
        var users = await _userManager.Users.Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.DisplayName ?? u.ArabicName ?? u.UserName ?? u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var list = rawList.Select(l => new
        {
            l.Id,
            l.Reference,
            l.Date,
            l.WarehouseName,
            l.ProductName,
            l.MovementType,
            l.TypeLabel,
            l.Qty,
            l.RunningBalance,
            l.UnitCost,
            l.TotalCost,
            l.Notes,
            CreatedBy = (!string.IsNullOrEmpty(l.CreatedByUserId) && users.TryGetValue(l.CreatedByUserId, out var uname))
                ? uname
                : "المدير (Administrator)"
        }).ToList();

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
        public string? UserId { get; set; }
        public string? UserName { get; set; }
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

        var currentUserId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("sub")?.Value
                         ?? req.UserId;

        if (string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(req.UserName))
        {
            var u = await _userManager.FindByNameAsync(req.UserName);
            if (u != null) currentUserId = u.Id;
        }

        if (string.IsNullOrEmpty(currentUserId))
        {
            var admin = await _userManager.FindByNameAsync("Administrator");
            if (admin != null) currentUserId = admin.Id;
        }

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
            CreatedByUserId = currentUserId
        };
        _db.StockLedger.Add(ledger);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, reference = refDoc, newQuantity = balance.QuantityOnHand });
    }

public class WarehouseProductUpdateDto
{
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public int? CategoryId { get; set; }
    public int BaseUnitId { get; set; }
    public decimal? MinStockLevel { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? ReorderQuantity { get; set; }
    public decimal? WeightedAverageCost { get; set; }
    public decimal? LastPurchaseCost { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProductUnitUpdateDto>? ProductUnits { get; set; }
}

public class ProductUnitUpdateDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public decimal ConversionFactorToBase { get; set; } = 1;
    public bool IsDefaultPurchaseUnit { get; set; }
    public bool IsDefaultRequisitionUnit { get; set; }
}

}
