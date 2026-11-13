using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Core.Entities.Warehouse;
using Pos.Repository.Data;
using Microsoft.AspNetCore.Identity;
using Pos.Repository.Identity;

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
    private readonly UserManager<AppUser> _userManager;

    public PurchaseOrdersController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ── Helper: get current user id & name ───────────────────────────────────
    private string? GetUserId() =>
        User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User?.FindFirst("sub")?.Value;

    private async Task<(string? Id, string Name)> GetCurrentUserInfoAsync()
    {
        var uid = GetUserId();
        if (string.IsNullOrEmpty(uid)) return (null, "النظام");
        var user = await _userManager.FindByIdAsync(uid);
        return (uid, user?.DisplayName ?? user?.UserName ?? "النظام");
    }

    private async Task AddLogAsync(int poId, string eventType,
        string? oldStatus, string? newStatus,
        string? description, string? userId, string? userName)
    {
        _db.PurchaseOrderLogs.Add(new PurchaseOrderLog
        {
            PurchaseOrderId = poId,
            EventType = eventType,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Description = description,
            CreatedByUserId = userId,
            CreatedByName = userName,
            EventDate = DateTime.UtcNow
        });
    }

    // ── GET all ──────────────────────────────────────────────────────────────
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
            StatusAr = p.Status == PurchaseOrderStatus.Draft ? "مسودة"
                     : p.Status == PurchaseOrderStatus.Approved ? "معتمد"
                     : p.Status == PurchaseOrderStatus.PartiallyReceived ? "مستلم جزئياً"
                     : p.Status == PurchaseOrderStatus.FullyReceived ? "مستلم بالكامل"
                     : "ملغي",
            p.OrderDate,
            p.ExpectedDeliveryDate,
            p.SupplierId,
            SupplierName = p.Supplier != null ? p.Supplier.NameAr : null,
            p.WarehouseId,
            WarehouseName = p.Warehouse != null ? p.Warehouse.NameAr : null,
            p.SubTotal,
            p.TaxAmount,
            p.TotalAmount,
            p.Notes,
            p.CreatedByUserId,
            LineCount = p.Lines.Count,
            GRNsCount = p.GoodsReceiptNotes.Count
        }).ToListAsync());
    }

    // ── GET by id ─────────────────────────────────────────────────────────────
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

    // ── GET audit log ─────────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("{id}/logs")]
    public async Task<IActionResult> GetLogs(int id)
    {
        var logs = await _db.PurchaseOrderLogs
            .Where(l => l.PurchaseOrderId == id)
            .OrderByDescending(l => l.EventDate)
            .Select(l => new
            {
                l.Id,
                l.EventType,
                l.OldStatus,
                l.NewStatus,
                l.Description,
                l.CreatedByName,
                l.EventDate
            }).ToListAsync();

        if (!logs.Any())
        {
            var po = await _db.PurchaseOrders.FindAsync(id);
            if (po != null)
            {
                var uname = "Administrator";
                if (!string.IsNullOrEmpty(po.CreatedByUserId))
                {
                    var u = await _userManager.FindByIdAsync(po.CreatedByUserId);
                    if (u != null) uname = u.DisplayName ?? u.ArabicName ?? u.UserName ?? uname;
                }

                var createdLog = new PurchaseOrderLog
                {
                    PurchaseOrderId = id,
                    EventType = "Created",
                    OldStatus = null,
                    NewStatus = "مسودة",
                    Description = $"تم إنشاء أمر الشراء {po.OrderNumber}",
                    CreatedByName = uname,
                    EventDate = po.OrderDate
                };
                _db.PurchaseOrderLogs.Add(createdLog);

                if (po.Status != PurchaseOrderStatus.Draft)
                {
                    var statusAr = po.Status switch
                    {
                        PurchaseOrderStatus.Approved => "معتمد",
                        PurchaseOrderStatus.PartiallyReceived => "مستلم جزئياً",
                        PurchaseOrderStatus.FullyReceived => "مستلم بالكامل",
                        PurchaseOrderStatus.Cancelled => "ملغي",
                        _ => po.Status.ToString()
                    };
                    var evType = po.Status == PurchaseOrderStatus.Cancelled ? "Cancelled" :
                                 (po.Status == PurchaseOrderStatus.FullyReceived || po.Status == PurchaseOrderStatus.PartiallyReceived ? "GrnCreated" : "Approved");

                    var currentLog = new PurchaseOrderLog
                    {
                        PurchaseOrderId = id,
                        EventType = evType,
                        OldStatus = "مسودة",
                        NewStatus = statusAr,
                        Description = $"تحديث حالة أمر الشراء إلى {statusAr}",
                        CreatedByName = uname,
                        EventDate = po.OrderDate.AddMinutes(5)
                    };
                    _db.PurchaseOrderLogs.Add(currentLog);
                }
                await _db.SaveChangesAsync();

                logs = await _db.PurchaseOrderLogs
                    .Where(l => l.PurchaseOrderId == id)
                    .OrderByDescending(l => l.EventDate)
                    .Select(l => new
                    {
                        l.Id,
                        l.EventType,
                        l.OldStatus,
                        l.NewStatus,
                        l.Description,
                        l.CreatedByName,
                        l.EventDate
                    }).ToListAsync();
            }
        }

        return Ok(logs);
    }

    // ── CREATE ────────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PurchaseOrder model)
    {
        var (uid, uname) = await GetCurrentUserInfoAsync();
        var year = DateTime.Now.Year;
        var count = await _db.PurchaseOrders.CountAsync(p => p.OrderDate.Year == year) + 1;
        model.OrderNumber = $"PO-{year}-{count:D4}";
        model.Status = PurchaseOrderStatus.Draft;
        model.OrderDate = DateTime.UtcNow;
        model.CreatedByUserId = uid;
        model.SubTotal = model.Lines.Sum(l => l.OrderedQuantity * l.UnitPrice);
        model.TotalAmount = model.SubTotal + model.TaxAmount;
        _db.PurchaseOrders.Add(model);
        await _db.SaveChangesAsync();

        await AddLogAsync(model.Id, "Created", null, "مسودة",
            $"تم إنشاء أمر الشراء {model.OrderNumber}", uid, uname);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = model.Id }, new { model.Id, model.OrderNumber });
    }

    // ── EDIT ──────────────────────────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Edit(int id, [FromBody] PurchaseOrder model)
    {
        var po = await _db.PurchaseOrders.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == id);
        if (po == null) return NotFound();
        if (po.Status != PurchaseOrderStatus.Draft)
            return BadRequest(new { message = "لا يمكن تعديل أمر شراء غير مسودة" });

        var (uid, uname) = await GetCurrentUserInfoAsync();

        po.SupplierId = model.SupplierId;
        po.WarehouseId = model.WarehouseId;
        po.ExpectedDeliveryDate = model.ExpectedDeliveryDate;
        po.Notes = model.Notes;
        po.TaxAmount = model.TaxAmount;

        // Replace lines
        _db.PurchaseOrderLines.RemoveRange(po.Lines);
        po.Lines = model.Lines;
        po.SubTotal = po.Lines.Sum(l => l.OrderedQuantity * l.UnitPrice);
        po.TotalAmount = po.SubTotal + po.TaxAmount;

        await AddLogAsync(id, "Edited", po.Status.ToString(), po.Status.ToString(),
            $"تم تعديل بيانات أمر الشراء", uid, uname);
        await _db.SaveChangesAsync();

        return Ok(new { message = "تم الحفظ" });
    }

    // ── APPROVE ───────────────────────────────────────────────────────────────
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var po = await _db.PurchaseOrders.FindAsync(id);
        if (po == null) return NotFound();
        if (po.Status != PurchaseOrderStatus.Draft)
            return BadRequest(new { message = "يمكن اعتماد أوامر الشراء في حالة المسودة فقط" });

        var (uid, uname) = await GetCurrentUserInfoAsync();
        var oldStatus = "مسودة";
        po.Status = PurchaseOrderStatus.Approved;

        await AddLogAsync(id, "Approved", oldStatus, "معتمد",
            $"تم اعتماد أمر الشراء", uid, uname);
        await _db.SaveChangesAsync();

        return Ok(new { message = "تم اعتماد أمر الشراء" });
    }

    // ── CANCEL ────────────────────────────────────────────────────────────────
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelPoDto? dto = null)
    {
        var po = await _db.PurchaseOrders.FindAsync(id);
        if (po == null) return NotFound();
        if (po.Status == PurchaseOrderStatus.FullyReceived)
            return BadRequest(new { message = "لا يمكن إلغاء أمر شراء مكتمل الاستلام" });

        var (uid, uname) = await GetCurrentUserInfoAsync();
        var oldStatusAr = po.Status == PurchaseOrderStatus.Draft ? "مسودة"
                        : po.Status == PurchaseOrderStatus.Approved ? "معتمد"
                        : po.Status == PurchaseOrderStatus.PartiallyReceived ? "مستلم جزئياً"
                        : "غير معروف";
        po.Status = PurchaseOrderStatus.Cancelled;

        await AddLogAsync(id, "Cancelled", oldStatusAr, "ملغي",
            $"تم إلغاء أمر الشراء. السبب: {dto?.Reason ?? "لم يُحدد"}", uid, uname);
        await _db.SaveChangesAsync();

        return Ok(new { message = "تم إلغاء أمر الشراء" });
    }

    // ── GRN - Goods Receipt Notes ─────────────────────────────────────────────
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

        var (uid, uname) = await GetCurrentUserInfoAsync();

        model.PurchaseOrderId = poId;
        model.SupplierId = po.SupplierId;
        model.WarehouseId = po.WarehouseId;
        model.ReceivedByUserId = uid;
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

            decimal baseQty = line.QuantityReceived;
            var pUnit = product.ProductUnits.FirstOrDefault(u => u.UnitId == line.UnitId);
            if (pUnit != null) baseQty *= pUnit.ConversionFactorToBase;

            var balance = await _db.StockBalances.FirstOrDefaultAsync(b => b.WarehouseId == model.WarehouseId && b.ProductId == line.ProductId);
            if (balance == null)
            {
                balance = new StockBalance { WarehouseId = model.WarehouseId, ProductId = line.ProductId };
                _db.StockBalances.Add(balance);
            }
            var oldValue = balance.QuantityOnHand * balance.WeightedAverageCost;
            var newValue = baseQty * line.UnitCost;
            balance.QuantityOnHand += baseQty;
            balance.WeightedAverageCost = balance.QuantityOnHand > 0 ? (oldValue + newValue) / balance.QuantityOnHand : line.UnitCost;
            balance.LastUpdated = DateTime.UtcNow;

            _db.StockLedger.Add(new StockLedger
            {
                WarehouseId = model.WarehouseId, ProductId = line.ProductId,
                MovementType = StockMovementType.GoodsReceipt,
                QuantityIn = baseQty, RunningBalance = balance.QuantityOnHand,
                UnitCost = line.UnitCost, TotalCost = baseQty * line.UnitCost,
                ReferenceDocumentType = "GRN", ReferenceDocumentNumber = model.GrnNumber,
                BatchNumber = line.BatchNumber, ExpiryDate = line.ExpiryDate,
                MovementDate = DateTime.UtcNow,
                CreatedByUserId = uid
            });

            product.WeightedAverageCost = balance.WeightedAverageCost;
            product.LastPurchaseCost = line.UnitCost;

            var poLine = po.Lines.FirstOrDefault(l => l.ProductId == line.ProductId);
            if (poLine != null) poLine.ReceivedQuantity += line.QuantityReceived;
        }

        bool allReceived = po.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity);
        var oldStatusAr = po.Status == PurchaseOrderStatus.Approved ? "معتمد" : "مستلم جزئياً";
        po.Status = allReceived ? PurchaseOrderStatus.FullyReceived : PurchaseOrderStatus.PartiallyReceived;
        var newStatusAr = allReceived ? "مستلم بالكامل" : "مستلم جزئياً";

        await _db.SaveChangesAsync();

        // Log GRN creation
        await AddLogAsync(poId, "GrnCreated", oldStatusAr, newStatusAr,
            $"تم إنشاء إذن استلام {model.GrnNumber} بواسطة {uname}", uid, uname);

        // Create supplier invoice
        var supplierInvoice = new SupplierInvoice
        {
            SupplierId = po.SupplierId,
            GoodsReceiptNoteId = model.Id,
            InvoiceNumber = model.SupplierInvoiceNumber ?? model.GrnNumber,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            TotalAmount = model.Lines.Sum(l => l.QuantityReceived * l.UnitCost)
        };
        _db.SupplierInvoices.Add(supplierInvoice);

        var supplier = await _db.WH_Suppliers.FindAsync(po.SupplierId);
        if (supplier != null) supplier.CurrentBalance += supplierInvoice.TotalAmount;

        await _db.SaveChangesAsync();
        return Ok(new { GrnId = model.Id, GrnNumber = model.GrnNumber });
    }
}

public class CancelPoDto
{
    public string? Reason { get; set; }
}
