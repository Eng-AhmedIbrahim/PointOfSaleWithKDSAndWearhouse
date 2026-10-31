using BlazorBase.ERPFrontServices.SettingsServices;
using Serilog;

namespace POS.Desktop.Services;

public class CallCenterNotificationService : IDisposable, IAsyncDisposable
{
    private readonly CallCenterHubSettings _hubSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HandelDeliveryInvocation _deliveryInvocation;
    private readonly DispatcherSettings _dispatcherSettings;
    private readonly List<HubConnection> _connections = new();
    private bool _isInitialized;

    public CallCenterNotificationService(
        CallCenterHubSettings hubSettings, 
        DispatcherSettings dispatcherSettings,
        IServiceScopeFactory scopeFactory,
        HandelDeliveryInvocation deliveryInvocation)
    {
        _hubSettings = hubSettings;
        _dispatcherSettings = dispatcherSettings;
        _scopeFactory = scopeFactory;
        _deliveryInvocation = deliveryInvocation;
    }

    /// <summary>
    /// Plays the embedded bell.wav sound for 1 second on Dispatcher machine.
    /// Falls back to System.Console.Beep or SystemSounds.Asterisk.
    /// </summary>
    private static void PlayBellForOneSecond()
    {
        Task.Run(async () =>
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("bell.wav"));

                if (resourceName != null)
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var player = new System.Media.SoundPlayer(stream);
                        player.Play();
                        await Task.Delay(1000);
                        player.Stop();
                        return;
                    }
                }
            }
            catch { /* fall through to backup */ }

            try { Console.Beep(1000, 1000); }
            catch { System.Media.SystemSounds.Asterisk.Play(); }
        });
    }

    public async Task InitializeAsync()
    {
        Log.Information("[CCHub] InitializeAsync called. _isInitialized={IsInit}, Machine={Machine}",
            _isInitialized, Environment.MachineName);

        if (_isInitialized)
        {
            Log.Information("[CCHub] Already initialized, skipping.");
            return;
        }

        _isInitialized = true;

        if (_hubSettings.Urls == null || !_hubSettings.Urls.Any())
        {
            Log.Error("[CCHub] No Hub URLs configured! Check appsettings.json -> CallCenterHubs:Urls");
            return;
        }

        Log.Information("[CCHub] Found {Count} Hub URL(s): {Urls}",
            _hubSettings.Urls.Count, string.Join(", ", _hubSettings.Urls));

        foreach (var url in _hubSettings.Urls)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Log.Warning("[CCHub] Skipping empty URL.");
                continue;
            }

            Log.Information("[CCHub] Building connection to: {Url}", url);

            var connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            // ── أحداث إعادة الاتصال ──────────────────────────────────────────────
            connection.Reconnecting  += ex => { Log.Warning("[CCHub] Reconnecting... {Msg}", ex?.Message); return Task.CompletedTask; };
            connection.Reconnected   += id => { Log.Information("[CCHub] Reconnected. ConnectionId={Id}", id); return Task.CompletedTask; };
            connection.Closed        += ex => { Log.Warning("[CCHub] Connection closed. {Msg}", ex?.Message); return Task.CompletedTask; };

            // ── Message Handlers ──────────────────────────────────────────────────
            connection.On<OrderDto>("ReceiveNewDeliveryOrder", (order) =>
            {
                Log.Information("[CCHub][MSG] ReceiveNewDeliveryOrder → OrderId={Id}, MachineName={Machine}",
                    order.OrderId, order.MachineName);

                if (order.MachineName == Environment.MachineName)
                {
                    Log.Information("[CCHub][SKIP] Same machine – skipping self-send.");
                    return;
                }

                Log.Information("[CCHub] Triggering ShowNotification for ReceiveNewDeliveryOrder #{Id}", order.OrderId);
                _deliveryInvocation.TriggerShowNotification(
                    $"جديد: طلب توصيل برقم {order.OrderId} من {order.CustomerName}", Severity.Info);
                _deliveryInvocation.TriggerNewOrderReceived();

                Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var settingsService = scope.ServiceProvider.GetRequiredService<ISystemSettingsServices>();
                        var settings = await settingsService.GetDispatcherSettingsAsync();

                        if (settings.IsDispatcher)
                        {
                            Log.Information("[CCHub] IsDispatcher=true → playing bell + printing for Order #{Id}", order.OrderId);
                            PlayBellForOneSecond();
                            var printService = scope.ServiceProvider.GetRequiredService<IPrintOrderService>();
                            await printService.PrintReceivedOrderAsync(order);
                        }
                        else
                        {
                            Log.Information("[CCHub] IsDispatcher=false → skip bell/print for Order #{Id}", order.OrderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "[CCHub] Dispatcher actions failed for Order #{Id}", order.OrderId);
                    }
                });
            });

            connection.On<OrderDto>("ReceiveOrderDispatched", (order) =>
            {
                Log.Information("[CCHub][MSG] ReceiveOrderDispatched → OrderId={Id}", order.OrderId);
                var branchInfo = !string.IsNullOrEmpty(order.BranchName) ? $" ({order.BranchName})" : "";
                _deliveryInvocation.TriggerShowNotification(
                    $"تم خروج الطلب رقم {order.OrderId}{branchInfo} مع السائق {order.DriverName}", Severity.Success);
                _deliveryInvocation.TriggerNewOrderReceived();
            });

            connection.On<OrderDto>("ReceiveOrderCollected", (order) =>
            {
                Log.Information("[CCHub][MSG] ReceiveOrderCollected → OrderId={Id}", order.OrderId);
                var branchInfo = !string.IsNullOrEmpty(order.BranchName) ? $" ({order.BranchName})" : "";
                _deliveryInvocation.TriggerShowNotification(
                    $"تم تسليم الطلب رقم {order.OrderId}{branchInfo} بنجاح", Severity.Info);
                _deliveryInvocation.TriggerNewOrderReceived();
            });

            connection.On<OrderDto>("OrderDispatchedCentralNotification", (order) =>
            {
                Log.Information("[CCHub][MSG] ✅ OrderDispatchedCentralNotification → OrderId={Id}", order.OrderId);
                var branchInfo = !string.IsNullOrEmpty(order.BranchName) ? $" ({order.BranchName})" : "";
                _deliveryInvocation.TriggerShowNotification(
                    $"تم إرسال الطلب رقم {order.OrderId}{branchInfo} للفرع بنجاح", Severity.Success);
                _deliveryInvocation.TriggerNewOrderReceived();
            });

            connection.On<OrderDto>("ReceiveDeliveryOrderSendSuccess", (order) =>
            {
                Log.Information("[CCHub][MSG] ReceiveDeliveryOrderSendSuccess → OrderId={Id}", order.OrderId);
                var branchInfo = !string.IsNullOrEmpty(order.BranchName) ? $" ({order.BranchName})" : "";
                _deliveryInvocation.TriggerShowNotification(
                    $"تم إرسال طلب التوصيل رقم {order.OrderId}{branchInfo} للفرع بنجاح", Severity.Success);
                _deliveryInvocation.TriggerNewOrderReceived();
            });

            connection.On<OrderDto, string>("ReceiveDeliveryOrderUnsend", (order, error) =>
            {
                Log.Information("[CCHub][MSG] ReceiveDeliveryOrderUnsend → OrderId={Id}, Error={Err}", order.OrderId, error);
                var branchInfo = !string.IsNullOrEmpty(order.BranchName) ? $" ({order.BranchName})" : "";
                _deliveryInvocation.TriggerShowNotification(
                    $"فشل إرسال طلب التوصيل رقم {order.OrderId}{branchInfo}: {error}", Severity.Error);
                _deliveryInvocation.TriggerNewOrderReceived();
            });

            connection.On<OrderDto, string>("OrderDispatchFailedCentralNotification", (order, error) =>
            {
                Log.Information("[CCHub][MSG] ❌ OrderDispatchFailedCentralNotification → OrderId={Id}, Error={Err}",
                    order.OrderId, error);
                var branchInfo = !string.IsNullOrEmpty(order.BranchName) ? $" ({order.BranchName})" : "";
                _deliveryInvocation.TriggerShowNotification(
                    $"فشل إرسال الطلب رقم {order.OrderId}{branchInfo} للفرع: {error}", Severity.Error);
                _deliveryInvocation.TriggerNewOrderReceived();
            });

            connection.On<OrderDto>("ReceiveOrderUpdated", async (order) =>
            {
                Log.Information("[CCHub][MSG] ReceiveOrderUpdated → OrderId={Id}, State={State}",
                    order.OrderId, order.OrderState);

                var branchInfo = !string.IsNullOrEmpty(order.BranchName) ? $" في {order.BranchName}" : "";

                string message = order.OrderState switch
                {
                    "Dispatched"              => $"تم خروج الطلب رقم {order.OrderId}{branchInfo} مع السائق {order.DriverName}",
                    "Completed"               => $"تم تسليم الطلب رقم {order.OrderId}{branchInfo} بنجاح",
                    "Voided"                  => $"تم إلغاء الطلب رقم {order.OrderId}{branchInfo}",
                    "FailedToDeliverToBranch" => $"فشل إرسال الطلب رقم {order.OrderId} للفرع",
                    "SentToBranch"            => $"تم إرسال الطلب رقم {order.OrderId} للفرع بنجاح",
                    _                         => $"تم تحديث حالة الطلب رقم {order.OrderId}{branchInfo} إلى {order.OrderState}"
                };

                var severity = order.OrderState switch
                {
                    "Completed"               => Severity.Success,
                    "Dispatched"              => Severity.Info,
                    "Voided"                  => Severity.Warning,
                    "FailedToDeliverToBranch" => Severity.Error,
                    _                         => Severity.Normal
                };

                if (order.OrderState == "FailedToDeliverToBranch" && _dispatcherSettings.SoundEnableCallCenter)
                    System.Media.SystemSounds.Exclamation.Play();
                else if (_dispatcherSettings.SoundEnableCallCenter)
                    PlayBellForOneSecond();

                // If this machine is the Dispatcher, print follow-up or void locally on Desktop
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var settingsService = scope.ServiceProvider.GetRequiredService<ISystemSettingsServices>();
                    var settings = await settingsService.GetDispatcherSettingsAsync();
                    if (settings.IsDispatcher)
                    {
                        var printService = scope.ServiceProvider.GetRequiredService<IPrintOrderService>();

                        // Detect item-level voids (partial void from Call Center)
                        var hasVoidedItems = (order.VoidedItems != null && order.VoidedItems.Any()) ||
                                             (order.OrderDetails != null && order.OrderDetails.Any(item => item.IsVoided == true || (item.VoidAmount.HasValue && item.VoidAmount > 0)));

                        if (order.OrderState == "Voided" || hasVoidedItems)
                        {
                            var voidedList = (order.VoidedItems != null && order.VoidedItems.Any())
                                ? order.VoidedItems
                                : order.OrderDetails?.Where(item => item.IsVoided == true || (item.VoidAmount.HasValue && item.VoidAmount > 0)).ToList();
                            if (voidedList == null || !voidedList.Any()) voidedList = order.OrderDetails;

                            if (voidedList != null && voidedList.Any())
                            {
                                Log.Information("[CCHub] Void detected for Order #{Id} ({Count} items) -> printing VOID tickets locally", order.OrderId, voidedList.Count);
                                PlayBellForOneSecond();
                                await printService.PrintVoidReceiptAsync(order, voidedList);
                            }
                        }
                        else if (order.IsFollowUp == true && (order.AddedItems != null && order.AddedItems.Any()))
                        {
                            PlayBellForOneSecond();
                            await printService.PrintFollowUpOrderAsync(order, order.AddedItems);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[CCHub] Error checking dispatcher settings or printing follow-up order locally");
                }

                _deliveryInvocation.TriggerShowNotification(message, severity);
                _deliveryInvocation.TriggerNewOrderReceived();
            });

            // ── Connect to Hub ────────────────────────────────────────────────────
            try
            {
                Log.Information("[CCHub] Starting connection to {Url} …", url);
                await connection.StartAsync();
                Log.Information("[CCHub] ✅ Connected! ConnectionId={Id}", connection.ConnectionId);
                _connections.Add(connection);

                try
                {
                    Log.Information("[CCHub] Registering device group: Machine={Machine}", Environment.MachineName);
                    await connection.InvokeAsync("RegisterDeviceGroup", Environment.MachineName, true);
                    Log.Information("[CCHub] ✅ Registered in SignalR group '{Machine}'.", Environment.MachineName);
                }
                catch (Exception groupEx)
                {
                    Log.Error(groupEx, "[CCHub] RegisterDeviceGroup failed");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[CCHub] ❌ Failed to connect to {Url}", url);
            }
        }

        Log.Information("[CCHub] InitializeAsync complete. Active connections={Count}", _connections.Count);
    }

    public void Dispose()
    {
        foreach (var connection in _connections)
            try { connection.DisposeAsync().GetAwaiter().GetResult(); } catch { }
        _connections.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections)
            await connection.DisposeAsync();
        _connections.Clear();
    }
}