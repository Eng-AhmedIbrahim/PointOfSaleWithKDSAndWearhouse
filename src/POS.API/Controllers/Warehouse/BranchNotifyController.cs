using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POS.API.Hubs;

namespace POS.API.Controllers.Warehouse;

/// <summary>
/// نقطة نهاية تستقبل إشعارات من سيرفر المخزن المركزي وتوزعها على الفروع المتصلة بهذا السيرفر عبر SignalR
/// يُستدعى من BranchRequisitionsController.NotifyBranch() في سيرفر المخزن
/// </summary>
[ApiController]
[Route("api/warehouse/branch-notify")]
public class BranchNotifyController : ControllerBase
{
    private readonly IHubContext<WarehouseHub> _hub;

    public BranchNotifyController(IHubContext<WarehouseHub> hub)
    {
        _hub = hub;
    }

    /// <summary>
    /// يستقبل الإشعار من سيرفر المخزن ويوزعه على العملاء المتصلين بهذا السيرفر
    /// POST /api/warehouse/branch-notify
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Notify([FromBody] BranchNotifyPayload payload)
    {
        if (payload == null) return BadRequest();

        // أرسل الإشعار لكل عملاء الفرع المتصلين بهذا السيرفر
        await _hub.Clients.Group($"branch_{payload.RequestingBranchId}")
            .SendAsync("RequisitionStatusChanged", new
            {
                payload.RequisitionId,
                payload.RequisitionNumber,
                payload.Status,
                payload.Message,
                payload.Timestamp
            });

        return Ok(new { message = "تم إرسال الإشعار للفرع" });
    }
}

public class BranchNotifyPayload
{
    public int RequisitionId { get; set; }
    public string? RequisitionNumber { get; set; }
    public string? Status { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
    public int RequestingBranchId { get; set; }
}