namespace Warehouse.Desktop.Services;

public class CallCenterNotificationService : IDisposable, IAsyncDisposable
{
    private readonly CallCenterHubSettings _hubSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HandelDeliveryInvocation _deliveryInvocation;
    private readonly DispatcherSettings _dispatcherSettings;
    private readonly List<HubConnection> _connections = new();

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

    public async Task InitializeAsync()
    {
        if (_hubSettings.Urls == null || !_hubSettings.Urls.Any())
            return;

        foreach (var url in _hubSettings.Urls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;

            var connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            connection.On<OrderDto>("ReceiveNewDeliveryOrder", (order) =>
            {
                _deliveryInvocation.TriggerShowNotification($"Ø¬Ø¯ÙŠØ¯: Ø·Ù„Ø¨ ØªÙˆØµÙŠÙ„ Ø¨Ø±Ù‚Ù… {order.OrderId} Ù…Ù† {order.CustomerName}", Severity.Info);
                
                _deliveryInvocation.TriggerNewOrderReceived();

                // If this machine is the dispatcher, print the order automatically
                if (_dispatcherSettings.IsDispatcher)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var printService = scope.ServiceProvider.GetRequiredService<IPrintOrderService>();
                                await printService.PrintReceivedOrderAsync(order);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to print order {order.OrderId} automatically: {ex.Message}");
                        }
                    });
                }
            });

            connection.On<OrderDto>("ReceiveOrderDispatched", (order) =>
            {
                _deliveryInvocation.TriggerShowNotification($"ØªÙ… Ø®Ø±ÙˆØ¬ Ø§Ù„Ø·Ù„Ø¨ Ø±Ù‚Ù… {order.OrderId} Ù…Ø¹ Ø§Ù„Ø³Ø§Ø¦Ù‚ {order.DriverName}", Severity.Success);
                _deliveryInvocation.TriggerNewOrderReceived(); // Refresh UI if needed
            });

            connection.On<OrderDto>("ReceiveOrderCollected", (order) =>
            {
                _deliveryInvocation.TriggerShowNotification($"ØªÙ… ØªØ³Ù„ÙŠÙ… Ø§Ù„Ø·Ù„Ø¨ Ø±Ù‚Ù… {order.OrderId} Ø¨Ù†Ø¬Ø§Ø­", Severity.Info);
                _deliveryInvocation.TriggerNewOrderReceived(); // Refresh UI if needed
            });

            try
            {
                await connection.StartAsync();
                _connections.Add(connection);
            }
            catch (Exception ex)
            {
                // Log and ignore
                Console.WriteLine($"Failed to connect to {url}: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            // Sync disposal for HubConnection is complex, but we can fire and forget or block
            // Here we use GetAwaiter().GetResult() as we are in a shutdown context
            try { connection.DisposeAsync().GetAwaiter().GetResult(); } catch { }
        }
        _connections.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            await connection.DisposeAsync();
        }
        _connections.Clear();
    }
}

