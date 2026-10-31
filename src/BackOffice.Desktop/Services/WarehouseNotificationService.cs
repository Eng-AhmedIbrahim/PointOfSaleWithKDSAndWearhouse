using Microsoft.AspNetCore.SignalR.Client;
using BlazorBase;
using MudBlazor;

namespace BackOffice.Desktop.Services;

/// <summary>
/// Connects BackOffice.Desktop to the WarehouseHub on POS.API.
/// Registers as branch group and listens for requisition status updates.
/// Follows the CallCenterNotificationService pattern.
/// </summary>
public class WarehouseNotificationService : IDisposable, IAsyncDisposable
{
    private readonly string _hubUrl;
    private readonly CommonProperties _commonProperties;
    private readonly WarehouseNotificationInvocation _warehouseInvocation;
    private HubConnection? _connection;
    private bool _initialized;

    public WarehouseNotificationService(
        BlazorBase.API.ApiSettings apiSettings,
        CommonProperties commonProperties,
        WarehouseNotificationInvocation warehouseInvocation)
    {
        _hubUrl = apiSettings.BaseUrl.TrimEnd('/') + "/hubs/warehouse";
        _commonProperties = commonProperties;
        _warehouseInvocation = warehouseInvocation;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) })
                .Build();

            // ── Event: warehouse changed the requisition status ──
            _connection.On<object>("RequisitionStatusChanged", (payload) =>
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    int requisitionId = root.TryGetProperty("requisitionId", out var rid) ? rid.GetInt32() : 0;
                    string status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";
                    string message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "";

                    _warehouseInvocation.TriggerRequisitionStatusChanged(requisitionId, status, message);
                    _warehouseInvocation.TriggerShowWarehouseNotification(
                        "تحديث طلب التوريد",
                        message,
                        GetSeverityForStatus(status));
                    _warehouseInvocation.TriggerRequisitionListRefresh();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WarehouseNotification] Error parsing RequisitionStatusChanged: {ex.Message}");
                }
            });

            // ── Event: generic warehouse alert ──
            _connection.On<object>("WarehouseAlert", (payload) =>
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    string alertType = root.TryGetProperty("alertType", out var at) ? at.GetString() ?? "تنبيه" : "تنبيه";
                    string message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "";
                    _warehouseInvocation.TriggerShowWarehouseNotification(alertType, message, Severity.Warning);
                }
                catch { }
            });

            // ── Reconnection: re-register branch group ──
            _connection.Reconnected += async (connectionId) =>
            {
                Console.WriteLine($"[WarehouseNotification] Reconnected: {connectionId}");
                await RegisterBranchGroupAsync();
            };

            await _connection.StartAsync();
            await RegisterBranchGroupAsync();

            Console.WriteLine($"[WarehouseNotification] Connected to {_hubUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WarehouseNotification] Failed to connect to {_hubUrl}: {ex.Message}");
        }
    }

    private async Task RegisterBranchGroupAsync()
    {
        if (_connection?.State != HubConnectionState.Connected) return;

        try
        {
            int branchId = _commonProperties.BranchDetails?.Id ?? 0;
            if (branchId > 0)
            {
                await _connection.InvokeAsync("RegisterBranch", branchId);
                Console.WriteLine($"[WarehouseNotification] Registered as branch_{branchId}");
            }
            else
            {
                // BranchDetails may not be loaded yet — retry after short delay
                await Task.Delay(3000);
                branchId = _commonProperties.BranchDetails?.Id ?? 0;
                if (branchId > 0)
                    await _connection.InvokeAsync("RegisterBranch", branchId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WarehouseNotification] RegisterBranch failed: {ex.Message}");
        }
    }

    private static Severity GetSeverityForStatus(string status) => status switch
    {
        "Approved" or "Received" => Severity.Success,
        "Rejected" or "Cancelled" => Severity.Error,
        "InTransit" or "Preparing" => Severity.Info,
        _ => Severity.Normal
    };

    public void Dispose()
    {
        try { _connection?.DisposeAsync().GetAwaiter().GetResult(); } catch { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}