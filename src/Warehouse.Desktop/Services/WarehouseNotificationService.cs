using Microsoft.AspNetCore.SignalR.Client;
using BlazorBase;
using MudBlazor;

namespace Warehouse.Desktop.Services;

/// <summary>
/// Connects Warehouse.Desktop to the WarehouseHub on POS.API.
/// Registers as warehouse group(s) and listens for new branch requisitions.
/// Follows the CallCenterNotificationService pattern.
/// </summary>
public class WarehouseNotificationService : IDisposable, IAsyncDisposable
{
    private readonly string _hubUrl;
    private readonly WarehouseNotificationInvocation _warehouseInvocation;
    private HubConnection? _connection;
    private bool _initialized;

    public WarehouseNotificationService(
        BlazorBase.API.ApiSettings apiSettings,
        WarehouseNotificationInvocation warehouseInvocation)
    {
        _hubUrl = apiSettings.BaseUrl.TrimEnd('/') + "/hubs/warehouse";
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

            // ── Event: new requisition received from a branch ──
            _connection.On<object>("NewRequisitionReceived", (payload) =>
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    int requisitionId = root.TryGetProperty("requisitionId", out var rid) ? rid.GetInt32() : 0;
                    string branchName = root.TryGetProperty("branchName", out var bn) ? bn.GetString() ?? "" : "";
                    string requisitionNumber = root.TryGetProperty("requisitionNumber", out var rn) ? rn.GetString() ?? "" : "";

                    try { System.Media.SystemSounds.Exclamation.Play(); } catch { }

                    _warehouseInvocation.TriggerNewRequisitionReceived(requisitionId, branchName, requisitionNumber);
                    _warehouseInvocation.TriggerShowWarehouseNotification(
                        "🔔 طلبية توريد جديدة",
                        $"وصلت طلبية توريد جديدة رقم {requisitionNumber} من {branchName}",
                        Severity.Warning);
                    _warehouseInvocation.TriggerRequisitionListRefresh();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WHNotification] Error parsing NewRequisitionReceived: {ex.Message}");
                }
            });

            // ── Event: requisition updated (branch confirmed/cancelled/received) ──
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
                    if (!string.IsNullOrEmpty(message))
                        _warehouseInvocation.TriggerShowWarehouseNotification("تحديث طلبية", message, Severity.Normal);
                    _warehouseInvocation.TriggerRequisitionListRefresh();
                }
                catch { }
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

            // ── Reconnection: re-register ──
            _connection.Reconnected += async (connectionId) =>
            {
                Console.WriteLine($"[WHNotification] Reconnected: {connectionId}");
                await RegisterWarehouseGroupsAsync();
            };

            await _connection.StartAsync();
            await RegisterWarehouseGroupsAsync();

            Console.WriteLine($"[WHNotification] Connected to {_hubUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WHNotification] Failed to connect to {_hubUrl}: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers to specific warehouse group(s). Call after loading warehouses list.
    /// If warehouseIds is null/empty, registers to default group warehouse_0.
    /// </summary>
    public async Task RegisterWarehouseGroupsAsync(IEnumerable<int>? warehouseIds = null)
    {
        if (_connection?.State != HubConnectionState.Connected) return;

        try
        {
            var ids = warehouseIds?.ToList();
            if (ids != null && ids.Count > 0)
            {
                foreach (var id in ids)
                {
                    await _connection.InvokeAsync("RegisterWarehouse", id);
                    Console.WriteLine($"[WHNotification] Registered as warehouse_{id}");
                }
            }
            else
            {
                // Default: register to warehouse_0 as catch-all
                await _connection.InvokeAsync("RegisterWarehouse", 0);
                Console.WriteLine($"[WHNotification] Registered as warehouse_0 (default)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WHNotification] RegisterWarehouse failed: {ex.Message}");
        }
    }

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