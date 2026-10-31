using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pos.Repository.Data;

namespace POS.API.Controllers.Warehouse;

[Authorize]
[ApiController]
[Route("api/warehouse/reports")]
public class WarehouseReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public WarehouseReportsController(AppDbContext db) => _db = db;

    [HttpGet("stock-valuation")]
    public async Task<IActionResult> StockValuation([FromQuery] int? warehouseId)
    {
        var query = _db.StockBalances.Include(b => b.Product).ThenInclude(p => p.BaseUnit).Include(b => b.Warehouse).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(b => b.WarehouseId == warehouseId.Value);
        var items = await query.Where(b => b.QuantityOnHand > 0).Select(b => new
        {
            b.WarehouseId, WarehouseName = b.Warehouse != null ? b.Warehouse.NameAr : null,
            b.ProductId, ProductName = b.Product != null ? b.Product.NameAr : null,
            SKU = b.Product != null ? b.Product.SKU : null,
            Unit = b.Product != null && b.Product.BaseUnit != null ? b.Product.BaseUnit.Symbol : null,
            b.QuantityOnHand, b.WeightedAverageCost,
            StockValue = b.QuantityOnHand * b.WeightedAverageCost
        }).OrderByDescending(b => b.StockValue).ToListAsync();
        return Ok(new { TotalValue = items.Sum(i => i.StockValue), ItemCount = items.Count, Items = items });
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock([FromQuery] int? warehouseId)
    {
        var query = _db.StockBalances.Include(b => b.Product).ThenInclude(p => p.BaseUnit).Include(b => b.Warehouse).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(b => b.WarehouseId == warehouseId.Value);
        var items = await query.Where(b => b.Product != null && b.QuantityOnHand <= b.Product.ReorderPoint)
            .Select(b => new
            {
                b.WarehouseId, WarehouseName = b.Warehouse != null ? b.Warehouse.NameAr : null,
                b.ProductId, ProductName = b.Product != null ? b.Product.NameAr : null,
                b.QuantityOnHand, b.QuantityReserved,
                ReorderPoint = b.Product != null ? b.Product.ReorderPoint : 0,
                ReorderQuantity = b.Product != null ? b.Product.ReorderQuantity : 0,
                Shortage = b.Product != null ? b.Product.ReorderPoint - b.QuantityOnHand : 0
            }).OrderByDescending(b => b.Shortage).ToListAsync();
        return Ok(items);
    }

    [HttpGet("item-movements")]
    public async Task<IActionResult> ItemMovements([FromQuery] int productId, [FromQuery] int? warehouseId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.StockLedger.Include(l => l.Warehouse).Where(l => l.ProductId == productId).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(l => l.WarehouseId == warehouseId.Value);
        if (from.HasValue) query = query.Where(l => l.MovementDate >= from.Value);
        if (to.HasValue) query = query.Where(l => l.MovementDate <= to.Value);
        var items = await query.OrderBy(l => l.MovementDate).Select(l => new
        {
            l.MovementDate, l.MovementType, l.QuantityIn, l.QuantityOut, l.RunningBalance,
            l.UnitCost, l.TotalCost, l.ReferenceDocumentType, l.ReferenceDocumentNumber,
            WarehouseName = l.Warehouse != null ? l.Warehouse.NameAr : null
        }).ToListAsync();
        return Ok(items);
    }

    [HttpGet("requisitions-summary")]
    public async Task<IActionResult> RequisitionsSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.BranchRequisitions.Include(r => r.RequestingBranch).Include(r => r.TargetWarehouse).AsQueryable();
        if (from.HasValue) query = query.Where(r => r.RequestDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.RequestDate <= to.Value);
        var items = await query.GroupBy(r => r.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
        return Ok(items);
    }

    [HttpGet("supplier-aging")]
    public async Task<IActionResult> SupplierAging()
    {
        var unpaid = await _db.SupplierInvoices.Include(i => i.Supplier)
            .Where(i => !i.IsFullyPaid).OrderBy(i => i.DueDate)
            .Select(i => new
            {
                i.SupplierId, SupplierName = i.Supplier != null ? i.Supplier.NameAr : null,
                i.InvoiceNumber, i.InvoiceDate, i.DueDate, i.TotalAmount, i.PaidAmount,
                Outstanding = i.TotalAmount - i.PaidAmount,
                DaysOverdue = (DateTime.Today - i.DueDate).Days
            }).ToListAsync();
        return Ok(unpaid);
    }
}
