using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using POS.Core.Entities.Warehouse;
using Pos.Repository.Data;
using POS.API.Hubs;

namespace POS.API.Controllers.Warehouse;

[Authorize]
[ApiController]
[Route("api/warehouse/requisitions")]
public class BranchRequisitionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<WarehouseHub> _hub;
    public BranchRequisitionsController(AppDbContext db, IHubContext<WarehouseHub> hub)
    { _db = db; _hub = hub; }

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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var r = await _db.BranchRequisitions
            .Include(x => x.RequestingBranch).Include(x => x.TargetWarehouse)
            .Include(x => x.ReroutedToWarehouse)
            .Include(x => x.Lines).ThenInclude(l => l.Product).ThenInclude(p => p!.BaseUnit)
            .Include(x => x.Lines).ThenInclude(l => l.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        return Ok(r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BranchRequisition model)
    {
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
            await _hub.Clients.Group($"warehouse_{model.TargetWarehouseId}").SendAsync("NewRequisitionReceived", new
            {
                RequisitionId = model.Id,
                RequisitionNumber = model.RequisitionNumber,
                BranchName = branch?.Name,
                Timestamp = DateTime.UtcNow
            });
        }

        return CreatedAtAction(nameof(GetById), new { id = model.Id }, new { model.Id, model.RequisitionNumber });
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var r = await _db.BranchRequisitions.Include(x => x.RequestingBranch).Include(x => x.TargetWarehouse).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (r.Status != RequisitionStatus.Draft) return BadRequest(new { message = "يمكن إرسال الطلب في حالة المسودة فقط" });
        r.Status = RequisitionStatus.Submitted;
        await _db.SaveChangesAsync();
        // Notify warehouse
        await _hub.Clients.Group($"warehouse_{r.TargetWarehouseId}").SendAsync("NewRequisitionReceived", new
        { RequisitionId = r.Id, RequisitionNumber = r.RequisitionNumber, BranchName = r.RequestingBranch?.Name, Timestamp = DateTime.UtcNow });
        return Ok(new { message = "تم إرسال الطلب للمخزن الرئيسي" });
    }

    [HttpPost("{id}/review")]
    public async Task<IActionResult> StartReview(int id)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Status != RequisitionStatus.Submitted) return BadRequest(new { message = "الطلب يجب أن يكون في حالة 'تم الإرسال'" });
        r.Status = RequisitionStatus.UnderReview;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "قيد المراجعة", "الطلب قيد المراجعة من قبل المخزن");
        return Ok();
    }

    [HttpPost("{id}/propose-modification")]
    public async Task<IActionResult> ProposeModification(int id, [FromBody] List<LineModificationDto> modifications)
    {
        var r = await _db.BranchRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        foreach (var mod in modifications)
        {
            var line = r.Lines.FirstOrDefault(x => x.Id == mod.LineId);
            if (line != null) { line.ProposedQuantity = mod.ProposedQuantity; line.ModificationNotes = mod.Notes; }
        }
        r.Status = RequisitionStatus.ModificationProposed;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "تعديل مقترح", "المخزن اقترح تعديل الكميات - يرجى المراجعة والموافقة");
        return Ok(new { message = "تم إرسال اقتراح التعديل للفرع" });
    }

    [HttpPost("{id}/accept-modification")]
    public async Task<IActionResult> AcceptModification(int id)
    {
        var r = await _db.BranchRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        foreach (var l in r.Lines.Where(x => x.ProposedQuantity.HasValue))
            l.RequestedQuantity = l.ProposedQuantity!.Value;
        r.Status = RequisitionStatus.UnderReview;
        await _db.SaveChangesAsync();
        await NotifyWarehouse(r, "موافقة على التعديل", "الفرع وافق على الكميات المعدلة");
        return Ok(new { message = "تمت الموافقة على التعديل" });
    }

    [HttpPost("{id}/reject-modification")]
    public async Task<IActionResult> RejectModification(int id, [FromBody] ReasonDto reason)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = RequisitionStatus.Cancelled;
        r.RejectionReason = reason.Reason;
        await _db.SaveChangesAsync();
        await NotifyWarehouse(r, "رفض التعديل", $"الفرع رفض التعديل: {reason.Reason}");
        return Ok();
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var r = await _db.BranchRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        foreach (var l in r.Lines) l.ApprovedQuantity = l.ProposedQuantity ?? l.RequestedQuantity;
        r.Status = RequisitionStatus.Approved;
        r.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "معتمد", "تم اعتماد طلبكم وجاري التحضير");
        return Ok(new { message = "تم اعتماد الطلب" });
    }

    [HttpPost("{id}/reroute")]
    public async Task<IActionResult> Reroute(int id, [FromBody] RerouteDto dto)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = RequisitionStatus.Rerouted;
        r.ReroutedToWarehouseId = dto.TargetWarehouseId;
        r.RerouteReason = dto.Reason;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "محول لمخزن آخر", $"تم تحويل طلبكم لمخزن آخر: {dto.Reason}");
        // Notify new warehouse
        await _hub.Clients.Group($"warehouse_{dto.TargetWarehouseId}").SendAsync("NewRequisitionReceived",
            new { RequisitionId = r.Id, RequisitionNumber = r.RequisitionNumber, IsRerouted = true, Timestamp = DateTime.UtcNow });
        return Ok();
    }

    [HttpPost("{id}/dispatch")]
    public async Task<IActionResult> Dispatch(int id, [FromBody] DispatchDto dto)
    {
        var r = await _db.BranchRequisitions.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (r.Status != RequisitionStatus.Approved && r.Status != RequisitionStatus.Preparing) return BadRequest();
        foreach (var line in dto.Lines)
        {
            var l = r.Lines.FirstOrDefault(x => x.Id == line.LineId);
            if (l != null) l.DispatchedQuantity = line.DispatchedQuantity;
        }
        r.Status = RequisitionStatus.InTransit;
        r.DispatchedAt = DateTime.UtcNow;
        r.DispatchedByUserId = dto.UserId;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "في الطريق", "تم شحن الطلبية وهي في الطريق إليكم");
        return Ok(new { message = "تم الشحن" });
    }

    [HttpPost("{id}/receive")]
    public async Task<IActionResult> Receive(int id, [FromBody] ReceiveDto dto)
    {
        var r = await _db.BranchRequisitions.Include(x => x.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (r.Status != RequisitionStatus.InTransit) return BadRequest();
        foreach (var line in dto.Lines)
        {
            var l = r.Lines.FirstOrDefault(x => x.Id == line.LineId);
            if (l != null)
            {
                l.ReceivedQuantity = line.ReceivedQuantity;
                l.DiscrepancyQuantity = l.DispatchedQuantity - line.ReceivedQuantity;
            }
        }
        r.Status = RequisitionStatus.Received;
        r.ReceivedAt = DateTime.UtcNow;
        r.ReceivedByUserId = dto.UserId;
        await _db.SaveChangesAsync();
        await NotifyWarehouse(r, "مستلم", $"الفرع أكد استلام الشحنة - {DateTime.Now:dd/MM/yyyy}");
        return Ok(new { message = "تم تأكيد الاستلام" });
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] ReasonDto reason)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = RequisitionStatus.Rejected;
        r.RejectionReason = reason.Reason;
        await _db.SaveChangesAsync();
        await NotifyBranch(r, "مرفوض", $"تم رفض الطلب: {reason.Reason}");
        return Ok();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] ReasonDto reason)
    {
        var r = await _db.BranchRequisitions.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Status == RequisitionStatus.InTransit || r.Status == RequisitionStatus.Received)
            return BadRequest(new { message = "لا يمكن إلغاء طلب في الطريق أو تم استلامه" });
        r.Status = RequisitionStatus.Cancelled;
        r.Notes = (r.Notes ?? "") + $" | إلغاء: {reason.Reason}";
        await _db.SaveChangesAsync();
        return Ok();
    }

    private async Task NotifyBranch(BranchRequisition r, string status, string msg)
        => await _hub.Clients.Group($"branch_{r.RequestingBranchId}").SendAsync("RequisitionStatusChanged",
            new { RequisitionId = r.Id, RequisitionNumber = r.RequisitionNumber, Status = status, Message = msg, Timestamp = DateTime.UtcNow });

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