namespace POS.API.Helpers;

public class OrderRetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderRetryBackgroundService> _logger;
    private readonly CallCenterSettings _centerSettings;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _retryInterval;

    public OrderRetryBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<OrderRetryBackgroundService> logger,
        CallCenterSettings centerSettings,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _centerSettings = centerSettings;
        _configuration = configuration;
        _retryInterval = TimeSpan.FromMinutes(_centerSettings!.SendRetryIntervalMessagesAfterByMinutes > 0 
            ? _centerSettings.SendRetryIntervalMessagesAfterByMinutes 
            : 1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Retry Service] Starting background retry service. IsCentral={IsCentral}, Interval={Interval}m", 
            _centerSettings.IsCentralCallCenter, _retryInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                if (_centerSettings.IsCentralCallCenter)
                {
                    await ProcessCentralFailedOrdersAsync(scope);
                }
                else
                {
                    await ProcessBranchPendingSyncAsync(scope);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retry background service cycle.");
            }

            await Task.Delay(_retryInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Central Call Center logic: Retries orders that failed to dispatch to branch (FailedToDeliverToBranch).
    /// </summary>
    private async Task ProcessCentralFailedOrdersAsync(AsyncServiceScope scope)
    {
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var branchService = scope.ServiceProvider.GetRequiredService<IBranchService>();

        var failedOrders = await orderService.GetFailedDeliveryOrdersAsync();

        if (failedOrders != null && failedOrders.Any())
        {
            foreach (Orders order in failedOrders)
            {
                var branch = await branchService.GetBranchByIdAsync(order.BranchID)!;
                var rawBranchUrl = !string.IsNullOrEmpty(order.DeliveryBranchUrl) ? order.DeliveryBranchUrl : branch?.ApiUrl;

                if (string.IsNullOrEmpty(rawBranchUrl))
                {
                    _logger.LogWarning("[Retry Service] Branch {BranchID} has no API URL. Skipping order {OrderID}.", order.BranchID, order.OrderID);
                    continue;
                }

                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(30);

                    var orderDto = await orderService.GetOrderDtoByIdAsync(order.Id);
                    if (orderDto == null) 
                    {
                        _logger.LogWarning("[Retry Service] Could not fetch DTO for Order ID {OrderId}. Skipping.", order.Id);
                        continue;
                    }

                    // Get Call Center API URL from configuration (prefer non-localhost ApiBaseUrl)
                    var callCenterUrl = _configuration["ApiBaseUrl"] ?? string.Empty;
                    if (callCenterUrl.Contains("localhost") || callCenterUrl.Contains("127.0.0.1"))
                    {
                        callCenterUrl = string.Empty;
                    }
                    if (string.IsNullOrWhiteSpace(callCenterUrl))
                    {
                        var urls = _configuration["Kestrel:Endpoints:Https:Url"] ?? _configuration["Kestrel:Endpoints:Http:Url"];
                        if (string.IsNullOrEmpty(urls))
                        {
                            urls = _configuration.GetValue<string>("ASPNETCORE_URLS") ?? "https://localhost:7142";
                        }
                        callCenterUrl = urls.Split(';')[0];
                    }
                    orderDto.CallCenterApiUrl = callCenterUrl.TrimEnd('/', '\\');
                    
                    // Reset state to Assigned before sending to branch
                    // so the branch doesn't save it as FailedToDeliverToBranch
                    orderDto.OrderState = OrderStates.Assigned.ToString();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var json = JsonSerializer.Serialize(orderDto, options);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var sanitizedUrl = rawBranchUrl?.Trim().TrimEnd('/', '\\') ?? string.Empty;
                    var targetEndpoint = $"{sanitizedUrl}/api/order/receiveDispatchedOrder";

                    _logger.LogInformation("[Retry Service] Retrying Order {OrderID} for Branch {BranchID} at {Endpoint}", order.OrderID, order.BranchID, targetEndpoint);

                    var response = await httpClient.PostAsync(targetEndpoint, content);
                    _logger.LogInformation("[Retry Service] Branch responded with StatusCode: {StatusCode} for Order {OrderID}", response.StatusCode, order.OrderID);

                    if (response.IsSuccessStatusCode)
                    {
                        var branchResponseJson = await response.Content.ReadAsStringAsync();
                        var branchOrderDto = JsonSerializer.Deserialize<OrderDto>(branchResponseJson, options);

                        order.OrderState = OrderStates.SentToBranch;
                        if (branchOrderDto != null)
                        {
                            order.CallCenterOrderId = branchOrderDto.Id;
                        }
                        await orderService.UpdateOrderAsync(order);
                        _logger.LogInformation("[Retry Service] SUCCESS: Order {OrderID} sent to branch {BranchID}.", order.OrderID, order.BranchID);

                        try
                        {
                            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<DeliveryHub>>();
                            orderDto.OrderState = OrderStates.SentToBranch.ToString();
                            if (!string.IsNullOrEmpty(orderDto.MachineName))
                            {
                                await hubContext.Clients.Group(orderDto.MachineName).SendAsync("OrderDispatchedCentralNotification", orderDto);
                            }
                            else
                            {
                                await hubContext.Clients.All.SendAsync("OrderDispatchedCentralNotification", orderDto);
                            }
                        }
                        catch (Exception hubEx)
                        {
                            _logger.LogError(hubEx, "[Retry Service] Error broadcasting SignalR notification for Order {OrderID}", order.OrderID);
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("[Retry Service] FAILED: Order {OrderID}. StatusCode: {StatusCode}. Error: {Error}", order.OrderID, response.StatusCode, errorContent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Retry Service] EXCEPTION: Order {OrderID} to URL: {Url}", order.OrderID, rawBranchUrl);
                }
            }
        }
    }

    /// <summary>
    /// Branch / Dispatcher logic: Retries order status updates that failed to sync to Call Center (due to offline CC / VPN drop).
    /// </summary>
    private async Task ProcessBranchPendingSyncAsync(AsyncServiceScope scope)
    {
        if (PendingCallCenterSyncQueue.IsEmpty)
        {
            return;
        }

        var pendingOrders = PendingCallCenterSyncQueue.GetAll();
        if (!pendingOrders.Any()) return;

        _logger.LogInformation("[Branch Retry] Found {Count} pending order(s) waiting to sync to Call Center.", pendingOrders.Count);

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(15);

        foreach (var orderDto in pendingOrders)
        {
            var targetUrl = orderDto.CallCenterApiUrl;
            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                targetUrl = _configuration["ApiBaseUrl"];
            }
            if (string.IsNullOrWhiteSpace(targetUrl)) continue;

            targetUrl = targetUrl.Trim().TrimEnd('/', '\\');

            try
            {
                var json = JsonSerializer.Serialize(orderDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PutAsync($"{targetUrl}/api/order/receiveOrderUpdate", content);

                if (response.IsSuccessStatusCode)
                {
                    PendingCallCenterSyncQueue.TryRemove(orderDto.Id);
                    _logger.LogInformation("[Branch Retry] SUCCESS: Synced Order #{OrderId} (State: {State}) to Call Center at {Url}.",
                        orderDto.OrderId, orderDto.OrderState, targetUrl);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[Branch Retry] Failed to sync Order #{OrderId} to Call Center at {Url}. Status: {StatusCode}, Error: {Error}",
                        orderDto.OrderId, targetUrl, response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Branch Retry] Call Center at {Url} is currently unreachable ({Msg}). Will retry in next cycle.",
                    targetUrl, ex.Message);
                // Call Center is offline; break early to avoid consecutive timeout delays
                break;
            }
        }
    }
}
