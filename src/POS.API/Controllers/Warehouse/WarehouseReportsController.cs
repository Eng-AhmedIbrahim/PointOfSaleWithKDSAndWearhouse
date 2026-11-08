using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pos.Repository.Data;

namespace POS.API.Controllers.Warehouse;

//[Authorize]
[ApiController]
[Route("api/warehouse/reports")]
[Route("api/warehousereports")]
public class WarehouseReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public WarehouseReportsController(AppDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet("stock-valuation")]
    public async Task<IActionResult> StockValuation([FromQuery] int? warehouseId)
    {
        var query = _db.StockBalances.Include(b => b.Product).ThenInclude(p => p.BaseUnit).Include(b => b.Warehouse).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(b => b.WarehouseId == warehouseId.Value);
        var items = await query.Where(b => b.QuantityOnHand > 0).Select(b => new
        {
            b.WarehouseId,
            WarehouseName = b.Warehouse != null ? b.Warehouse.NameAr : null,
            b.ProductId,
            ProductName = b.Product != null ? b.Product.NameAr : null,
            SKU = b.Product != null ? b.Product.SKU : null,
            ProductCode = b.Product != null ? b.Product.SKU : null,
            Category = b.Product != null ? b.Product.CategoryName : null,
            Unit = b.Product != null && b.Product.BaseUnit != null ? b.Product.BaseUnit.NameAr : null,
            UnitName = b.Product != null && b.Product.BaseUnit != null ? b.Product.BaseUnit.NameAr : null,
            b.QuantityOnHand,
            OnHandQty = b.QuantityOnHand,
            b.WeightedAverageCost,
            AvgCostPrice = b.WeightedAverageCost,
            StockValue = b.QuantityOnHand * b.WeightedAverageCost,
            TotalValue = b.QuantityOnHand * b.WeightedAverageCost
        }).OrderByDescending(b => b.StockValue).ToListAsync();
        return Ok(items);
    }

    [AllowAnonymous]
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
    [AllowAnonymous]
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

    [AllowAnonymous]
    [HttpGet("supplier-aging")]
    public async Task<IActionResult> SupplierAging()
    {
        var unpaid = await _db.SupplierInvoices.Include(i => i.Supplier)
            .Where(i => i.TotalAmount > i.PaidAmount).OrderBy(i => i.DueDate)
            .Select(i => new
            {
                i.SupplierId, SupplierName = i.Supplier != null ? i.Supplier.NameAr : null,
                i.InvoiceNumber, i.InvoiceDate, i.DueDate, i.TotalAmount, i.PaidAmount,
                Outstanding = i.TotalAmount - i.PaidAmount,
                DaysOverdue = (DateTime.Today - i.DueDate).Days
            }).ToListAsync();
        return Ok(unpaid);
    }

    [AllowAnonymous]
    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        // 1. Total Valuation
        var totalValuation = await _db.StockBalances
            .Where(b => b.QuantityOnHand > 0)
            .SumAsync(b => (decimal?)(b.QuantityOnHand * b.WeightedAverageCost)) ?? 0m;

        // 2. Active Warehouses
        var activeWarehouses = await _db.Warehouses.Where(w => w.IsActive).CountAsync();
        var mainWarehouses = await _db.Warehouses.Where(w => w.IsActive && w.Type == POS.Core.Entities.Warehouse.WarehouseType.Main).CountAsync();
        var branchWarehouses = Math.Max(0, activeWarehouses - mainWarehouses);

        // 3. In-transit Shipments
        var inTransitCount = await _db.BranchRequisitions
            .Where(r => r.Status == POS.Core.Entities.Warehouse.RequisitionStatus.InTransit).CountAsync();

        // 4. Low stock items
        var lowStockCount = await _db.StockBalances
            .Include(b => b.Product)
            .Where(b => b.Product != null && b.QuantityOnHand <= b.Product.ReorderPoint)
            .CountAsync();

        // 5. Recent Requisitions
        var pendingRequisitions = await _db.BranchRequisitions
            .Include(r => r.RequestingBranch)
            .Where(r => r.Status != POS.Core.Entities.Warehouse.RequisitionStatus.Received && 
                        r.Status != POS.Core.Entities.Warehouse.RequisitionStatus.Cancelled && 
                        r.Status != POS.Core.Entities.Warehouse.RequisitionStatus.Rejected)
            .OrderByDescending(r => r.RequestDate)
            .Take(5)
            .Select(r => new
            {
                r.Id,
                RequisitionNumber = !string.IsNullOrEmpty(r.RequisitionNumber) ? r.RequisitionNumber : $"REQ-{r.Id:D4}",
                BranchName = r.RequestingBranch != null ? r.RequestingBranch.Name : "فرع",
                Date = r.RequestDate,
                Status = r.Status.ToString()
            }).ToListAsync();

        // 6. Recent Movements
        var recentMovements = await _db.StockLedger
            .Include(l => l.Product)
            .Include(l => l.Warehouse)
            .OrderByDescending(l => l.MovementDate)
            .Take(5)
            .Select(l => new
            {
                ProductName = l.Product != null ? (l.Product.NameAr ?? l.Product.NameEn) : "صنف",
                WarehouseName = l.Warehouse != null ? l.Warehouse.NameAr : "مستودع",
                MovementType = l.MovementType.ToString(),
                Qty = l.QuantityIn > 0 ? l.QuantityIn : -l.QuantityOut
            }).ToListAsync();

        return Ok(new
        {
            TotalValuation = totalValuation,
            ActiveWarehouses = activeWarehouses,
            MainWarehouses = mainWarehouses,
            BranchWarehouses = branchWarehouses,
            InTransitCount = inTransitCount,
            LowStockCount = lowStockCount,
            PendingRequisitions = pendingRequisitions,
            RecentMovements = recentMovements
        });
    }
}
