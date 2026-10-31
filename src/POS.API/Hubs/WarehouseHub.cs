namespace POS.API.Hubs;

/// <summary>
/// SignalR Hub for real-time Warehouse notifications
/// Handles requisition lifecycle, transfers, GRN events
/// </summary>
public class WarehouseHub : Hub
{
    /// <summary>
    /// Branch registers to receive notifications for its own requisitions
    /// </summary>
    public async Task RegisterBranch(int branchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"branch_{branchId}");
    }

    /// <summary>
    /// Warehouse staff registers to receive all warehouse notifications
    /// </summary>
    public async Task RegisterWarehouse(int warehouseId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"warehouse_{warehouseId}");
    }

    /// <summary>
    /// Notify a branch that their requisition status changed
    /// </summary>
    public async Task NotifyBranchRequisitionUpdate(int branchId, int requisitionId, string newStatus, string message)
    {
        await Clients.Group($"branch_{branchId}").SendAsync("RequisitionStatusChanged", new
        {
            RequisitionId = requisitionId,
            Status = newStatus,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify warehouse of new incoming requisition
    /// </summary>
    public async Task NotifyWarehouseNewRequisition(int warehouseId, int requisitionId, string branchName, string requisitionNumber)
    {
        await Clients.Group($"warehouse_{warehouseId}").SendAsync("NewRequisitionReceived", new
        {
            RequisitionId = requisitionId,
            BranchName = branchName,
            RequisitionNumber = requisitionNumber,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast to all warehouse users - e.g. low stock alert
    /// </summary>
    public async Task BroadcastWarehouseAlert(string alertType, string message, int? warehouseId = null)
    {
        if (warehouseId.HasValue)
            await Clients.Group($"warehouse_{warehouseId.Value}").SendAsync("WarehouseAlert", new { AlertType = alertType, Message = message, Timestamp = DateTime.UtcNow });
        else
            await Clients.All.SendAsync("WarehouseAlert", new { AlertType = alertType, Message = message, Timestamp = DateTime.UtcNow });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            Console.WriteLine($"[WarehouseHub] Client disconnected with error: {exception.Message}");
        await base.OnDisconnectedAsync(exception);
    }
}
