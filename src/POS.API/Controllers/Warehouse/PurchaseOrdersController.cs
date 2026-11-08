using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Core.Entities.Warehouse;
using Pos.Repository.Data;

namespace POS.API.Controllers.Warehouse;

//[Authorize]
[ApiController]
[Route("api/warehouse/purchase-orders")]
[Route("api/warehouse/purchaseorders")]
[Route("api/purchaseorders")]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public PurchaseOrdersController(AppDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PurchaseOrderStatus? status)
    {
        var query = _db.PurchaseOrders.Include(p => p.Supplier).Include(p => p.Warehouse).Include(p => p.Lines).AsQueryable();
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        return Ok(await query.OrderByDescending(p => p.OrderDate).Select(p => new
        {
            p.Id,
            OrderNumber = p.OrderNumber,
            PONumber = p.OrderNumber,
            Status = p.Status.ToString(),
            p.OrderDate,
            p.ExpectedDeliveryDate,
            p.SupplierId,
            SupplierName = p.Supplier != null ? p.Supplier.NameAr : null,
            p.WarehouseId,
            WarehouseName = p.Warehouse != null ? p.Warehouse.NameAr : null,
            p.SubTotal,
            p.TaxAmount,
            p.TotalAmount,
            LineCount = p.Lines.Count,
            GRNsCount = p.GoodsReceiptNotes.Count
        }).ToListAsync());
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var po = await _db.PurchaseOrders.Include(x => x.Supplier).Include(x => x.Warehouse)
            .Include(x => x.Lines).ThenInclude(l => l.Product).ThenInclude(p => p!.BaseUnit)
            .Include(x => x.Lines).ThenInclude(l => l.Unit)
            .Include(x => x.GoodsReceiptNotes)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (po == null) return NotFound();
        return Ok(po);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PurchaseOrder model)
    {
        var year = DateTime.Now.Year;
        var count = await _db.PurchaseOrders.CountAsync(p => p.OrderDate.Year == year) + 1;
        model.OrderNumber = $"PO-{year}-{count:D4}";
        model.Status = PurchaseOrderStatus.Draft;
        model.OrderDate = DateTime.UtcNow;
        // Calculate totals
        model.SubTotal = model.Lines.Sum(l => l.OrderedQuantity * l.UnitPrice);
        model.TotalAmount = model.SubTotal + model.TaxAmount;
        _db.PurchaseOrders.Add(model);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = model.Id }, new { model.Id, model.OrderNumber });
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var po = await _db.PurchaseOrders.FindAsync(id);
        if (po == null) return NotFound();
        if (po.Status != PurchaseOrderStatus.Draft) return BadRequest(new { message = "يمكن اعتماد أوامر الشراء في حالة المسودة فقط" });
        po.Status = PurchaseOrderStatus.Approved;
        await _db.SaveChangesAsync();
        return Ok(new { message = "تم اعتماد أمر الشراء" });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var po = await _db.PurchaseOrders.FindAsync(id);
        if (po == null) return NotFound();
        if (po.Status == PurchaseOrderStatus.FullyReceived) return BadRequest(new { message = "لا يمكن إلغاء أمر شراء مكتمل الاستلام" });
        po.Status = PurchaseOrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        return Ok();
    }

    // GRN - Goods Receipt Notes
    [AllowAnonymous]
    [HttpGet("{poId}/grn")]
    public async Task<IActionResult> GetGRNs(int poId)
        => Ok(await _db.GoodsReceiptNotes.Include(g => g.Lines).ThenInclude(l => l.Product)
            .Where(g => g.PurchaseOrderId == poId).OrderByDescending(g => g.ReceiptDate).ToListAsync());

    [HttpPost("{poId}/grn")]
    public async Task<IActionResult> CreateGRN(int poId, [FromBody] GoodsReceiptNote model)
    {
        var po = await _db.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == poId);
        if (po == null) return NotFound();
        if (po.Status != PurchaseOrderStatus.Approved && po.Status != PurchaseOrderStatus.PartiallyReceived)
            return BadRequest(new { message = "أمر الشراء يجب أن يكون معتمداً" });

        model.PurchaseOrderId = poId;
        model.SupplierId = po.SupplierId;
        model.WarehouseId = po.WarehouseId;
        var year = DateTime.Now.Year;
        var count = await _db.GoodsReceiptNotes.CountAsync() + 1;
        model.GrnNumber = $"GRN-{year}-{count:D4}";
        model.ReceiptDate = DateTime.UtcNow;
        _db.GoodsReceiptNotes.Add(model);

        // Update stock balances and ledger for each received line
        foreach (var line in model.Lines)
        {
            var product = await _db.WarehouseProducts.Include(p => p.ProductUnits).FirstOrDefaultAsync(p => p.Id == line.ProductId);
            if (product == null) continue;

            // Convert received qty to base unit
            decimal baseQty = line.QuantityReceived;
            var pUnit = product.ProductUnits.FirstOrDefault(u => u.UnitId == line.UnitId);
            if (pUnit != null) baseQty *= pUnit.ConversionFactorToBase;

            // Update or create StockBalance
            var balance = await _db.StockBalances.FirstOrDefaultAsync(b => b.WarehouseId == model.WarehouseId && b.ProductId == line.ProductId);
            if (balance == null)
            {
                balance = new StockBalance { WarehouseId = model.WarehouseId, ProductId = line.ProductId };
                _db.StockBalances.Add(balance);
            }
            // Weighted average cost calculation
            var oldValue = balance.QuantityOnHand * balance.WeightedAverageCost;
            var newValue = baseQty * line.UnitCost;
            balance.QuantityOnHand += baseQty;
            balance.WeightedAverageCost = balance.QuantityOnHand > 0 ? (oldValue + newValue) / balance.QuantityOnHand : line.UnitCost;
            balance.LastUpdated = DateTime.UtcNow;

            // Write StockLedger entry
            _db.StockLedger.Add(new StockLedger
            {
                WarehouseId = model.WarehouseId, ProductId = line.ProductId,
                MovementType = StockMovementType.GoodsReceipt,
                QuantityIn = baseQty, RunningBalance = balance.QuantityOnHand,
                UnitCost = line.UnitCost, TotalCost = baseQty * line.UnitCost,
                ReferenceDocumentType = "GRN", ReferenceDocumentNumber = model.GrnNumber,
                BatchNumber = line.BatchNumber, ExpiryDate = line.ExpiryDate,
                MovementDate = DateTime.UtcNow
            });

            // Update product cost
            product.WeightedAverageCost = balance.WeightedAverageCost;
            product.LastPurchaseCost = line.UnitCost;

            // Update PO line received qty
            var poLine = po.Lines.FirstOrDefault(l => l.ProductId == line.ProductId);
            if (poLine != null) poLine.ReceivedQuantity += line.QuantityReceived;
        }

        // Update PO status
        bool allReceived = po.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity);
        po.Status = allReceived ? PurchaseOrderStatus.FullyReceived : PurchaseOrderStatus.PartiallyReceived;

        // Update supplier invoice
        var supplierInvoice = new SupplierInvoice
        {
            SupplierId = po.SupplierId, GoodsReceiptNoteId = model.Id,
            InvoiceNumber = model.SupplierInvoiceNumber ?? model.GrnNumber,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            TotalAmount = model.Lines.Sum(l => l.QuantityReceived * l.UnitCost)
        };
        _db.SupplierInvoices.Add(supplierInvoice);

        // Update supplier balance
        var supplier = await _db.WH_Suppliers.FindAsync(po.SupplierId);
        if (supplier != null) supplier.CurrentBalance += supplierInvoice.TotalAmount;

        await _db.SaveChangesAsync();
        return Ok(new { GrnId = model.Id, GrnNumber = model.GrnNumber });
    }
}
