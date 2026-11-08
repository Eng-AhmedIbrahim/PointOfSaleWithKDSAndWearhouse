using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using POS.Core.Entities.Warehouse;
using Pos.Repository.Data;
using POS.API.Hubs;

namespace POS.API.Controllers.Warehouse;

//[Authorize]
[ApiController]
[Route("api/warehouse/requisitions")]
[Route("api/requisitions")]
[Route("api/branchrequisitions")]
public class BranchRequisitionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<WarehouseHub> _hub;
    public BranchRequisitionsController(AppDbContext db, IHubContext<WarehouseHub> hub)
    { _db = db; _hub = hub; }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RequisitionStatus? status, [FromQuery] int? branchId, [FromQuery] int? warehouseId)
    {
        var query = _db.BranchRequisitions
            .Include(r => r.RequestingBranch).Include(r => r.TargetWarehouse)
            .Include(r => r.ReroutedToWarehouse).Include(r => r.Lines).AsQueryable();
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (branchId.HasValue) query = query.Where(r => r.RequestingBranchId == branchId.Value);
        if (warehouseId.HasValue) query = query.Where(r => r.TargetWarehouseId == warehouseId.Value || r.ReroutedToWarehouseId == warehouseId.Value);
        var result = await query.OrderByDescending(r => r.RequestDate).Select(r => new
        {
            r.Id, r.RequisitionNumber, r.Status, r.RequestDate, r.RequiredDate,
            r.RequestingBranchId, BranchName = r.RequestingBranch != null ? r.RequestingBranch.Name : null,
            r.TargetWarehouseId, WarehouseName = r.TargetWarehouse != null ? r.TargetWarehouse.NameAr : null,
            r.ReroutedToWarehouseId, r.RerouteReason, r.Notes, r.RejectionReason,
            r.ApprovedAt, r.DispatchedAt, r.ReceivedAt,
            LineCount = r.Lines.Count
        }).ToListAsync();
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var r = await _db.BranchRequisitions
            .Include(x => x.RequestingBranch).Include(x => x.TargetWarehouse)
            .Include(x => x.ReroutedToWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.Product).ThenInclude(p => p!.BaseUnit)
            .Include(x => x.Lines).ThenInclude(l => l.Unit)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();

        var dto = new
        {
            r.Id,
            r.RequisitionNumber,
            r.RequestingBranchId,
            BranchName = r.RequestingBranch != null ? r.RequestingBranch.Name : null,
            r.TargetWarehouseId,
            TargetWarehouse = r.TargetWarehouse != null ? new
            {
                r.TargetWarehouse.Id,
                r.TargetWarehouse.Code,
                r.TargetWarehouse.NameAr,
                r.TargetWarehouse.NameEn
            } : null,
            r.Status,
            r.RequestDate,
            r.RequiredDate,
            r.Notes,
            r.RejectionReason,
            LineCount = r.Lines.Count,
            Lines = r.Lines.Select(l => new
            {
                l.Id,
                l.ProductId,
                Product = l.Product != null ? new
                {
                    l.Product.Id,
                    l.Product.SKU,
                    l.Product.NameAr,
                    l.Product.NameEn
                } : null,
                l.UnitId,
                Unit = l.Unit != null ? new
                {
                    l.Unit.Id,
                    l.Unit.NameAr,
                    l.Unit.NameEn
                } : null,
                l.RequestedQuantity,
                l.ProposedQuantity,
                l.ApprovedQuantity,
                l.DispatchedQuantity,
                l.ReceivedQuantity,
                l.DiscrepancyQuantity
            }).ToList()
        };

        return Ok(dto);
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BranchRequisition model)
    {
        // 1. Ensure valid RequestingBranchId (prevent FK conflict if 0 or non-existent)
        if (model.RequestingBranchId <= 0 || !await _db.Branches.AnyAsync(b => b.Id == model.RequestingBranchId))
        {
            var defaultBranch = await _db.Branches.FirstOrDefaultAsync();
            if (defaultBranch != null)
            {
                model.RequestingBranchId = defaultBranch.Id;
            }
            else
            {
                return BadRequest(new { message = "Ù„Ø§ ØªÙˆØ¬Ø¯ ÙØ±ÙˆØ¹ Ù…Ø³Ø¬Ù„Ø© Ø¨Ø§Ù„Ù†Ø¸Ø§Ù… Ù„Ø±Ø¨Ø· Ø·Ù„Ø¨ Ø§Ù„ØªÙˆØ±ÙŠØ¯ Ø¨Ù‡Ø§" });
            }
        }

        // 2. Ensure valid TargetWarehouseId
        if (model.TargetWarehouseId <= 0 || !await _db.Warehouses.AnyAsync(w => w.Id == model.TargetWarehouseId))
        {
            var defaultWarehouse = await _db.Warehouses.FirstOrDefaultAsync();
            if (defaultWarehouse != null)
            {
                model.TargetWarehouseId = defaultWarehouse.Id;
            }
            else
            {
                return BadRequest(new { message = "Ø§Ù„Ù…Ø³ØªÙˆØ¯Ø¹ Ø§Ù„Ù…Ø³ØªÙ‡Ø¯Ù ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯" });
            }
        }

        // Generate requisition number
        var year = DateTime.Now.Year;
        var count = await _db.BranchRequisitions.CountAsync(r => r.RequestDate.Year == year) + 1;
        model.RequisitionNumber = $"REQ-{year}-{count:D4}";
        if (model.Status == default)
            model.Status = RequisitionStatus.Draft;
        model.RequestDate = DateTime.UtcNow;
        _db.BranchRequisitions.Add(model);
        await _db.SaveChangesAsync();

        // If submitted immediately, notify warehouse
        if (model.Status == RequisitionStatus.Submitted)
        {
            var branch = await _db.Branches.FindAsync(model.RequestingBranchId);
            var payload = new
            {
                RequisitionId = model.Id,
                RequisitionNumber = model.RequisitionNumber,
                BranchName = branch?.Name ?? "Ø§Ù„ÙØ±Ø¹",
                Timestamp = DateTime.UtcNow
            };
            await _hub.Clients.All.SendAsync("NewRequisitionReceived", payload);
        }

        return CreatedAtAction(nameof(GetById), new { id = model.Id }, new { model.Id, model.RequisitionNumber });
    }

    [AllowAnonymous]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BranchRequisition model)
    {
        var existing = await _db.BranchRequisitions
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (existing == null) return NotFound();

        if (existing.Status != RequisitionStatus.Draft 
            && existing.Status != RequisitionStatus.Submitted 
            && existing.Status != RequisitionStatus.PendingSync
            && existing.Status != RequisitionStatus.ModificationProposed)
        {
            return BadRequest(new { message = "Ù„Ø§ ÙŠÙ…ÙƒÙ† ØªØ¹Ø¯ÙŠÙ„ Ø§Ù„Ø·Ù„Ø¨ÙŠØ© Ø¨Ø¹Ø¯ Ø§Ø¹ØªÙ…Ø§Ø¯Ù‡Ø§ Ø£Ùˆ Ø´Ø­Ù†Ù‡Ø§" });
        }

        existing.RequiredDate = model.RequiredDate;
        existing.Notes = model.Notes;
        if (model.TargetWarehouseId > 0)
            existing.TargetWarehouseId = model.TargetWarehouseId;

        // Replace lines
        _db.BranchRequisitionLines.RemoveRange(existing.Lines);
        existing.Lines = model.Lines.Select(l => new BranchRequisitionLine
        {
            RequisitionId = existing.Id,
            ProductId = l.ProductId,
            UnitId = l.UnitId,
            RequestedQuantity = l.RequestedQuantity
        }).ToList();

        await _db.SaveChangesAsync();

        // Notify warehouse of requisition update
        var branch = await _db.Branches.FindAsync(existing.RequestingBranchId);
        await _hub.Clients.All.SendAsync("RequisitionUpdated", new
        {
            RequisitionId = existing.Id,
            RequisitionNumber = existing.RequisitionNumber,
            Status = "Modified",
            Message = $"ØªÙ… ØªØ¹Ø¯ÙŠÙ„ Ø§Ù„Ø·Ù„Ø¨ÙŠØ© {existing.RequisitionNumber} Ù…Ù† ÙØ±Ø¹ {branch?.Name}",
            Timestamp = DateTime.UtcNow
        });

        return Ok(new { message = "ØªÙ… ØªØ¹Ø¯ÙŠÙ„ Ø§Ù„Ø·Ù„Ø¨ÙŠØ© Ø¨Ù†Ø¬Ø§Ø­" });
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var r = await _db.BranchRequisitions.Include(x => x.RequestingBranch).Include(x => x.TargetWarehouse).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (r.Status != RequisitionStatus.Draft) return BadRequest(new { message = "\u064a\u0645\u0643\u0646 \u062a\u0642\u062f\u064a\u0645 \u0627\u0644\u0637\u0644\u0628 \u0641\u0642\u0637 \u0645\u0646 \u062d\u0627\u0644\u0629 \u0645\u0633\u0648\u062f\u0629" });
        r.Status = RequisitionStatus.Submitted;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group($"warehouse_{r.TargetWarehouseId}").SendAsync("NewRequisitionReceived",
            new { RequisitionId = r.Id, RequisitionNumber = r.RequisitionNumber, BranchName = r.RequestingBranch?.Name, Timestamp = DateTime.UtcNow });
        return Ok(new { message = "\u062a\u0645 \u062a\u0642\u062f\u064a\u0645 \u0627\u0644\u0637\u0644\u0628 \u0628\u0646\u062c\u0627\u062d \u0648\u0625\u062e\u0637\u0627\u0631 \u0627\u0644\u0645\u0633\u062a\u0648\u062f\u0639" });
    }

    [HttpPost("{id}/review")]
    public async Task<IActionResult> StartReview(int id)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Status != RequisitionStatus.Submitted) return BadRequest();
        r.Status = RequisitionStatus.UnderReview;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "\u0642\u064a\u062f \u0627\u0644\u0645\u0631\u0627\u062c\u0639\u0629", "\u0637\u0644\u0628\u0643 \u0642\u064a\u062f \u0627\u0644\u0645\u0631\u0627\u062c\u0639\u0629 \u0645\u0646 \u0642\u0628\u0644 \u0627\u0644\u0645\u0633\u062a\u0648\u062f\u0639");
        return Ok();
    }

    [HttpPost("{id}/propose")]
    public async Task<IActionResult> ProposeModification(int id, [FromBody] List<LineModificationDto> lines)
    {
        var r = await _db.BranchRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        foreach (var mod in lines)
        {
            var l = r.Lines.FirstOrDefault(x => x.Id == mod.LineId);
            if (l != null) { l.ProposedQuantity = mod.ProposedQuantity; l.ModificationNotes = mod.Notes; }
        }
        r.Status = RequisitionStatus.ModificationProposed;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "\u062a\u0639\u062f\u064a\u0644 \u0645\u0642\u062a\u0631\u062d", "\u0627\u0644\u0645\u0633\u062a\u0648\u062f\u0639 \u0627\u0642\u062a\u0631\u062d \u062a\u0639\u062f\u064a\u0644\u0627\u062a \u0639\u0644\u0649 \u0637\u0644\u0628\u0643 - \u064a\u0631\u062c\u0649 \u0627\u0644\u0645\u0631\u0627\u062c\u0639\u0629");
        return Ok();
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var r = await _db.BranchRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        foreach (var l in r.Lines)
            l.ApprovedQuantity = l.ProposedQuantity ?? l.RequestedQuantity;
        r.Status = RequisitionStatus.Approved;
        r.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "\u0645\u0639\u062a\u0645\u062f", "\u062a\u0645 \u0627\u0639\u062a\u0645\u0627\u062f \u0637\u0644\u0628\u0643 \u0648\u064a\u062c\u0631\u064a \u062a\u062c\u0647\u064a\u0632 \u0627\u0644\u0628\u0636\u0627\u0639\u0629");
        return Ok();
    }

    [HttpPost("{id}/reroute")]
    public async Task<IActionResult> Reroute(int id, [FromBody] RerouteDto dto)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        r.ReroutedToWarehouseId = dto.TargetWarehouseId;
        r.RerouteReason = dto.Reason;
        r.Status = RequisitionStatus.Rerouted;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "\u0645\u062d\u0648\u0644", $"\u062a\u0645 \u062a\u062d\u0648\u064a\u0644 \u0637\u0644\u0628\u0643 \u0644\u0645\u0633\u062a\u0648\u062f\u0639 \u0622\u062e\u0631: {dto.Reason}");
        return Ok();
    }

    [HttpPost("{id}/prepare")]
    public async Task<IActionResult> StartPreparing(int id)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = RequisitionStatus.Preparing;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "\u0642\u064a\u062f \u0627\u0644\u062a\u062c\u0647\u064a\u0632", "\u062c\u0627\u0631 \u062a\u062c\u0647\u064a\u0632 \u0637\u0644\u0628\u064a\u062a\u0643");
        return Ok();
    }

    [HttpPost("{id}/dispatch")]
    public async Task<IActionResult> Dispatch(int id, [FromBody] DispatchDto dto)
    {
        var r = await _db.BranchRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        foreach (var dl in dto.Lines)
        {
            var l = r.Lines.FirstOrDefault(x => x.Id == dl.LineId);
            if (l != null) l.DispatchedQuantity = dl.DispatchedQuantity;
        }
        r.Status = RequisitionStatus.InTransit;
        r.DispatchedAt = DateTime.UtcNow;
        r.DispatchedByUserId = dto.UserId;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "\u0641\u064a \u0627\u0644\u0637\u0631\u064a\u0642", "\u062a\u0645 \u0625\u0631\u0633\u0627\u0644 \u0645\u062d\u062a\u0648\u064a\u0627\u062a \u0637\u0644\u0628\u064a\u062a\u0643 \u0648\u0647\u064a \u0641\u064a \u0627\u0644\u0637\u0631\u064a\u0642 \u0625\u0644\u064a\u0643");
        return Ok(new { message = "\u062a\u0645 \u0627\u0644\u0625\u0631\u0633\u0627\u0644" });
    }

    [HttpPost("{id}/receive")]
    public async Task<IActionResult> Receive(int id, [FromBody] ReceiveDto dto)
    {
        var r = await _db.BranchRequisitions
            .Include(x => x.Lines)
                .ThenInclude(l => l.Product)
                    .ThenInclude(p => p!.ProductUnits)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (r.Status != RequisitionStatus.InTransit) return BadRequest();

        // ===== Update received quantities =====
        foreach (var lineDto in dto.Lines)
        {
            var l = r.Lines.FirstOrDefault(x => x.Id == lineDto.LineId);
            if (l != null)
            {
                l.ReceivedQuantity = lineDto.ReceivedQuantity;
                l.DiscrepancyQuantity = l.DispatchedQuantity - lineDto.ReceivedQuantity;
            }
        }

        r.Status = RequisitionStatus.Received;
        r.ReceivedAt = DateTime.UtcNow;
        r.ReceivedByUserId = dto.UserId;

        // ===== Auto-update branch sub-warehouse inventory =====
        foreach (var line in r.Lines.Where(l => l.ReceivedQuantity > 0))
        {
            var product = line.Product;
            if (product == null) continue;

            // Calculate unit conversion factor: requested unit -> base unit
            decimal convFactor = 1m;
            if (line.UnitId != product.BaseUnitId)
            {
                var pu = product.ProductUnits.FirstOrDefault(u => u.UnitId == line.UnitId);
                if (pu != null && pu.ConversionFactorToBase > 0)
                    convFactor = pu.ConversionFactorToBase;
            }
            decimal convertedQty = line.ReceivedQuantity * convFactor;

            // Match item in MenuSalesItems: by Barcode -> ArabicName
            POS.Core.Entities.Item.MenuSalesItems? menuItem = null;
            if (!string.IsNullOrWhiteSpace(product.Barcode))
                menuItem = await _db.MenuSalesItems.FirstOrDefaultAsync(m => m.Barcode == product.Barcode);
            if (menuItem == null && !string.IsNullOrWhiteSpace(product.NameAr))
                menuItem = await _db.MenuSalesItems.FirstOrDefaultAsync(m => m.ArabicName == product.NameAr);

            if (menuItem == null)
            {
                // Auto-create raw material menu item so the branch inventory is never skipped
                menuItem = new POS.Core.Entities.Item.MenuSalesItems
                {
                    ArabicName = product.NameAr,
                    EnglishName = !string.IsNullOrWhiteSpace(product.NameEn) ? product.NameEn : product.NameAr,
                    NormalizedEnglishName = (!string.IsNullOrWhiteSpace(product.NameEn) ? product.NameEn : product.NameAr).ToUpperInvariant(),
                    Barcode = product.Barcode,
                    Price = 0,
                    Invisible = true,
                    ItemTypeId = 2, // Raw Material
                    IsInventory = true,
                    CreationDate = DateTime.UtcNow,
                    HasAttribute = false,
                    ByWeight = false
                };
                _db.MenuSalesItems.Add(menuItem);
                await _db.SaveChangesAsync();
            }

            // Find or create InventoryItem for this branch
            int branchId = r.RequestingBranchId;
            var invItem = await _db.InventoryItems
                .FirstOrDefaultAsync(inv => inv.MenuSalesItemId == menuItem.Id && inv.BranchId == branchId);

            if (invItem == null)
            {
                invItem = new POS.Core.Entities.Item.InventoryItem
                {
                    MenuSalesItemId = menuItem.Id,
                    BranchId = branchId,
                    CurrentQuantity = 0,
                    MinimumQuantity = 0,
                    TrackInventory = true,
                    ItemNameAr = menuItem.ArabicName,
                    ItemNameEn = menuItem.EnglishName,
                    CreatedAt = DateTime.UtcNow
                };
                _db.InventoryItems.Add(invItem);
                await _db.SaveChangesAsync(); // Flush to get Id
            }

            invItem.CurrentQuantity += convertedQty;
            invItem.UpdatedAt = DateTime.UtcNow;

            // Record StockIn transaction
            _db.InventoryTransactions.Add(new POS.Core.Entities.Item.InventoryTransaction
            {
                InventoryItemId = invItem.Id,
                Type = POS.Core.Entities.Item.TransactionType.StockIn,
                QuantityChange = convertedQty,
                ResultingQuantity = invItem.CurrentQuantity,
                ReferenceId = r.RequisitionNumber,
                Notes = $"Receive requisition {r.RequisitionNumber} - {product.NameAr}",
                CreatedBy = dto.UserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        await NotifyWarehouse(r, "\u0645\u0633\u062a\u0644\u0645", $"\u062a\u0645 \u062a\u0633\u0644\u064a\u0645 \u0627\u0644\u0641\u0631\u0639 \u0627\u0633\u062a\u0644\u0627\u0645 \u0627\u0644\u0637\u0644\u0628\u064a\u0627\u062a \u0628\u062a\u0627\u0631\u064a\u062e {DateTime.Now:dd/MM/yyyy}");
        return Ok(new { message = "\u062a\u0645 \u062a\u0633\u062c\u064a\u0644 \u0627\u0644\u0627\u0633\u062a\u0644\u0627\u0645 \u0648\u062a\u062d\u062f\u064a\u062b \u0645\u062e\u0632\u0648\u0646 \u0627\u0644\u0641\u0631\u0639 \u062a\u0644\u0642\u0627\u0626\u064a\u0627\u064b" });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] ReasonDto reason)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = RequisitionStatus.Rejected;
        r.RejectionReason = reason.Reason;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "\u0645\u0631\u0641\u0648\u0636", $"\u062a\u0645 \u0631\u0641\u0636 \u0637\u0644\u0628\u0643: {reason.Reason}");
        return Ok();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] ReasonDto reason)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Status == RequisitionStatus.InTransit || r.Status == RequisitionStatus.Received)
            return BadRequest(new { message = "\u0644\u0627 \u064a\u0645\u0643\u0646 \u0625\u0644\u063a\u0627\u0621 \u0637\u0644\u0628 \u0641\u064a \u0627\u0644\u0637\u0631\u064a\u0642 \u0623\u0648 \u062a\u0645 \u0627\u0633\u062a\u0644\u0627\u0645\u0647" });
        r.Status = RequisitionStatus.Cancelled;
        r.Notes = (r.Notes ?? "") + $" | \u0625\u0644\u063a\u0627\u0621: {reason.Reason}";
        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Notify branch of requisition status change via:
    /// 1. SignalR (same server)
    /// 2. HTTP POST to branch ApiUrl (cross-server)
    /// </summary>
    private async Task NotifyBranch(BranchRequisition r, string status, string msg)
    {
        var payload = new
        {
            RequisitionId = r.Id,
            RequisitionNumber = r.RequisitionNumber,
            Status = status,
            Message = msg,
            Timestamp = DateTime.UtcNow
        };

        // 1. SignalR for same-server branches
        await _hub.Clients.Group($"branch_{r.RequestingBranchId}")
            .SendAsync("RequisitionStatusChanged", payload);

        // 2. HTTP for cross-server branches via ApiUrl
        var branch = await _db.Branches.FindAsync(r.RequestingBranchId);
        if (!string.IsNullOrWhiteSpace(branch?.ApiUrl))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    await client.PostAsJsonAsync($"{branch.ApiUrl.TrimEnd('/')}/api/warehouse/branch-notify", payload);
                }
                catch { /* fire and forget */ }
            });
        }
    }

    private async Task NotifyWarehouse(BranchRequisition r, string status, string msg)
    {
        var wId = r.ReroutedToWarehouseId ?? r.TargetWarehouseId;
        await _hub.Clients.Group($"warehouse_{wId}").SendAsync("RequisitionUpdated",
            new { RequisitionId = r.Id, RequisitionNumber = r.RequisitionNumber, Status = status, Message = msg, Timestamp = DateTime.UtcNow });
    }
}

public record LineModificationDto(int LineId, decimal ProposedQuantity, string? Notes);
public record ReasonDto(string Reason);
public record RerouteDto(int TargetWarehouseId, string Reason);
public record DispatchLineDto(int LineId, decimal DispatchedQuantity);
public record DispatchDto(List<DispatchLineDto> Lines, string? UserId);
public record ReceiveLineDto(int LineId, decimal ReceivedQuantity);
public record ReceiveDto(List<ReceiveLineDto> Lines, string? UserId);
