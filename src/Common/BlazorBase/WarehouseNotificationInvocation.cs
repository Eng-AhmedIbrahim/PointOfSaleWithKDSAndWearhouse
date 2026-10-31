using MudBlazor;

namespace BlazorBase;

/// <summary>
/// Singleton event bus for Warehouse ↔ Branch SignalR notifications.
/// Mirrors the HandelDeliveryInvocation pattern.
/// </summary>
public class WarehouseNotificationInvocation
{
    // ── Branch side: fired when warehouse changes a requisition status ──
    public event Action<int, string, string>? OnRequisitionStatusChanged;

    // ── Warehouse side: fired when a branch submits a new requisition ──
    public event Action<int, string, string>? OnNewRequisitionReceived;

    // ── Both sides: generic warehouse alert with title+message+severity ──
    public event Action<string, string, Severity>? OnShowWarehouseNotification;

    // ── Both sides: trigger list refresh ──
    public event Action? OnRequisitionListRefresh;

    public void TriggerRequisitionStatusChanged(int requisitionId, string status, string message)
        => OnRequisitionStatusChanged?.Invoke(requisitionId, status, message);

    public void TriggerNewRequisitionReceived(int requisitionId, string branchName, string requisitionNumber)
        => OnNewRequisitionReceived?.Invoke(requisitionId, branchName, requisitionNumber);

    public void TriggerShowWarehouseNotification(string title, string message, Severity severity)
        => OnShowWarehouseNotification?.Invoke(title, message, severity);

    public void TriggerRequisitionListRefresh()
        => OnRequisitionListRefresh?.Invoke();
}