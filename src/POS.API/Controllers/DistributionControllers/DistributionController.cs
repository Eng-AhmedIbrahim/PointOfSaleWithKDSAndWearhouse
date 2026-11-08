using POS.Contract.Dtos.DineIn;

namespace POS.API.Controllers.DistributionControllers;

public class DistributionController : BaseApiController
{
    private readonly IDistributionServices _distributionService;
    private readonly IMapper _mapper;
    private readonly ILogger<DistributionController> _logger;
    private readonly IDeliveryOrderApiServices _deliveryOrderApiServices;
    private readonly IOrderApiServices _orderApiServices;
    private readonly AppDbContext _dbContext;
    private readonly IOrderService _orderService;
    private readonly IHubContext<DeliveryHub> _hubContext;
    private readonly CallCenterSettings _callCenterSettings;
    private readonly IConfiguration _configuration;

    public DistributionController(IDistributionServices distributionService,
        IMapper mapper,
        ILogger<DistributionController> logger,
        IDeliveryOrderApiServices deliveryOrderApiServices,
        IOrderApiServices orderApiServices,
        IOrderService orderService,
        AppDbContext dbContext,
        IHubContext<DeliveryHub> hubContext,
        CallCenterSettings callCenterSettings,
        IConfiguration configuration
        )
    {

        _distributionService = distributionService;
        _mapper = mapper;
        _logger = logger;
        _deliveryOrderApiServices = deliveryOrderApiServices;
        _orderApiServices = orderApiServices;
        _orderService = orderService;
        _dbContext = dbContext;
        _hubContext = hubContext;
        _callCenterSettings = callCenterSettings;
        _configuration = configuration;
    }

    [HttpGet("GetUnCompletedDeliveryOrders")]
    public async Task<IActionResult> GetUnCompletedDeliveryOrders()
    {
        var orders = await _distributionService.GetUnCompletedDeliveryOrdersAsync();

        if (orders is null || !orders.Any())
            return NotFound(new ApiResponse(404, "No uncompleted delivery orders found."));

        var orderDtos = _mapper.Map<ICollection<Orders>, ICollection<OrderDto>>(orders);

        return Ok(orderDtos);
    }

    [HttpPut("dispatch")]
    public async Task<IActionResult> DispatchOrder([FromBody] OrderDto orderDto)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var result = await _distributionService.UpdateDeliveryOrdersAfterDispatchAsync(orderDto);

            if (!result)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse(500, "Failed to update order."));
            }

            if (!orderDto?.SkipPrintingOnServer ?? false )
            {
                await PrintCustomerCloseDeliveryReceipt(
                    orderDto!,
                    null!,
                    orderDto!.OrderSettings!.FirstOrDefault(o => o.OrderType == OrderTypes.Delivery.ToString())
                );
            }

            await transaction.CommitAsync();
            
            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveOrderDispatched", orderDto);

            // Fetch the updated order from DB to ensure DTO has all calculated fields
            var updatedOrder = await _orderService.GetOrderByIdAsync(orderDto!.Id);
            if (updatedOrder != null)
            {
                var syncDto = _mapper.Map<OrderDto>(updatedOrder);
                // Prefer CallCenterApiUrl/CallCenterOrderId from DB (set during ReceiveDispatchedOrderAsync)
                // Fall back to values from incoming orderDto if DB has null/0
                syncDto.CallCenterApiUrl = !string.IsNullOrEmpty(updatedOrder.CallCenterApiUrl)
                    ? updatedOrder.CallCenterApiUrl
                    : orderDto.CallCenterApiUrl;
                syncDto.CallCenterOrderId = (updatedOrder.CallCenterOrderId.HasValue && updatedOrder.CallCenterOrderId > 0)
                    ? updatedOrder.CallCenterOrderId
                    : orderDto.CallCenterOrderId;

                syncDto.DriverID = updatedOrder.DriverID ?? orderDto.DriverID;
                syncDto.DriverName = updatedOrder.DriverName ?? orderDto.DriverName;
                syncDto.OrderState = updatedOrder.OrderState.ToString();

                _logger.LogInformation("[Branch Dispatch] Syncing Order {OrderId} to CC. CallCenterApiUrl={Url}, CallCenterOrderId={CCId}, State={State}",
                    syncDto.OrderId, syncDto.CallCenterApiUrl, syncDto.CallCenterOrderId, syncDto.OrderState);

                await SendUpdateToCallCenter(syncDto);
            }

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to dispatch order and print receipt.");
            return StatusCode(500, new ApiResponse(500, "An error occurred while dispatching order."));
        }
    }

    private async Task PrintCustomerCloseDeliveryReceipt(OrderDto orderDto, Orders createdOrder, OrderSettingToReturnDto? orderSettings)
    {
        List<string> branchDetails = await _orderApiServices.GetBranchDetails(orderDto);

        var printResult = await _deliveryOrderApiServices.GenerateAndPrintDeliveryReceipts(orderDto, createdOrder, branchDetails, true);
    }

    [HttpPut("collect-delivery")]
    public async Task<IActionResult> CollectDeliveryOrder([FromBody] OrderDto orderDto)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderDto.Id);
            if (order == null)
            {
                return NotFound(new ApiResponse(404, "Order not found."));
            }

            if (order.OrderState != OrderStates.Voided)
            {
                order.OrderState = (order.VoidAmount ?? 0) > 0 ? OrderStates.Voided : OrderStates.Completed;
            }
            order.BackTime = orderDto.BackTime ?? DateTime.Now;
            order.ClosingTime = DateTime.Now;
            order.CollectorID = orderDto.CollectorID;
            order.CollectorName = orderDto.CollectorName;

            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            // Update orderDto for SignalR
            orderDto.OrderState = order.OrderState.ToString();
            orderDto.ClosingTime = order.ClosingTime;
            orderDto.BackTime = order.BackTime;
            orderDto.CollectorID = order.CollectorID;
            orderDto.CollectorName = order.CollectorName;

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveOrderCollected", orderDto);

            // Send update to Call Center if this is a branch (prefer DB values set during ReceiveDispatchedOrderAsync)
            var syncDto = _mapper.Map<OrderDto>(order);
            syncDto.CallCenterApiUrl = !string.IsNullOrEmpty(order.CallCenterApiUrl)
                ? order.CallCenterApiUrl
                : orderDto.CallCenterApiUrl;
            syncDto.CallCenterOrderId = (order.CallCenterOrderId.HasValue && order.CallCenterOrderId > 0)
                ? order.CallCenterOrderId
                : orderDto.CallCenterOrderId;
            _logger.LogInformation("[Branch Collect] Syncing Order {OrderId} to CC. CallCenterApiUrl={Url}, CallCenterOrderId={CCId}",
                syncDto.OrderId, syncDto.CallCenterApiUrl, syncDto.CallCenterOrderId);
            await SendUpdateToCallCenter(syncDto);

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to Collect delivery order receipt.");
            return StatusCode(500, new ApiResponse(500, "An error occurred while Collecting Delivery order."));
        }
    }

    [HttpPut("un-dispatch/{id}")]
    public async Task<IActionResult> UnDispatchOrder(int id)
    {
        var result = await _distributionService.UnDispatchOrderAsync(id);

        if (!result)
            return BadRequest(new ApiResponse(500, "Failed to un-dispatch order."));

        // Notify via SignalR (we need minimal order info or just the ID)
        await _hubContext.Clients.All.SendAsync("ReceiveOrderUnDispatched", id);

        // Fetch order to sync with Call Center (CallCenterApiUrl and CallCenterOrderId stored in DB)
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order != null)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            orderDto.DriverID = null;
            orderDto.DriverName = null;
            orderDto.AssignTime = null;
            orderDto.DispatchID = null;

            _logger.LogInformation("[Branch UnDispatch] Syncing Order {OrderId} to CC. CallCenterApiUrl={Url}, CallCenterOrderId={CCId}",
                orderDto.OrderId, orderDto.CallCenterApiUrl, orderDto.CallCenterOrderId);
            await SendUpdateToCallCenter(orderDto);
        }

        return Ok(true);
    }

    [HttpPut("collect-driver-orders/{driverId}")]
    public async Task<IActionResult> CollectDriverOrders(string driverId)
    {
        var orders = await _distributionService.GetOrdersByDriverAsync(driverId);
        
        foreach (var order in orders)
        {
            if (order.OrderState != OrderStates.Voided)
            {
                order.OrderState = (order.VoidAmount ?? 0) > 0 ? OrderStates.Voided : OrderStates.Completed;
            }
            order.ClosingTime = DateTime.Now;
            _dbContext.Orders.Update(order);
        }

        await _dbContext.SaveChangesAsync();

        foreach (var order in orders)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            await _hubContext.Clients.All.SendAsync("ReceiveOrderCollected", orderDto);
            await SendUpdateToCallCenter(orderDto);
        }

        return Ok(true);
    }

    [HttpPut("collect-all")]
    public async Task<IActionResult> CollectAllOrders()
    {
        var orders = await _distributionService.GetUnCompletedDeliveryOrdersAsync();
        var dispatchedOrders = orders.Where(o => o.OrderState == OrderStates.Dispatched).ToList();

        foreach (var order in dispatchedOrders)
        {
            if (order.OrderState != OrderStates.Voided)
            {
                order.OrderState = (order.VoidAmount ?? 0) > 0 ? OrderStates.Voided : OrderStates.Completed;
            }
            order.ClosingTime = DateTime.Now;
            _dbContext.Orders.Update(order);
        }

        await _dbContext.SaveChangesAsync();

        foreach (var order in dispatchedOrders)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            await _hubContext.Clients.All.SendAsync("ReceiveOrderCollected", orderDto);
            await SendUpdateToCallCenter(orderDto);
        }

        return Ok(true);
    }

    private string ResolveCallCenterApiUrl(string? currentUrl)
    {
        // 1. PRIMARY: Use the per-order CallCenterApiUrl (set by CC when dispatching to branch).
        //    This is the most authoritative source — the CC explicitly told us its own URL.
        if (!string.IsNullOrWhiteSpace(currentUrl) &&
            !currentUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
            !currentUrl.Contains("127.0.0.1"))
        {
            return currentUrl.Trim().TrimEnd('/', '\\');
        }

        // 2. FALLBACK: Use configured ApiBaseUrl only if no per-order URL is available.
        //    WARNING: On branch machines, ApiBaseUrl may point to the branch itself — not the CC.
        //    This fallback should only trigger if the order has no stored CallCenterApiUrl.
        var apiBaseUrl = _configuration?["ApiBaseUrl"];
        if (!string.IsNullOrWhiteSpace(apiBaseUrl) &&
            !apiBaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
            !apiBaseUrl.Contains("127.0.0.1"))
        {
            return apiBaseUrl.Trim().TrimEnd('/', '\\');
        }

        // 3. Last resort: Check CallCenterHubs:Urls:0 (strip /callcenterhub suffix)
        var hubUrl = _configuration?["CallCenterHubs:Urls:0"];
        if (!string.IsNullOrWhiteSpace(hubUrl) &&
            !hubUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
            !hubUrl.Contains("127.0.0.1"))
        {
            var cleaned = hubUrl.Trim().TrimEnd('/', '\\');
            var idx = cleaned.IndexOf("/callcenterhub", StringComparison.OrdinalIgnoreCase);
            if (idx > 0) cleaned = cleaned.Substring(0, idx);
            return cleaned;
        }

        return currentUrl?.Trim().TrimEnd('/', '\\') ?? string.Empty;
    }

    private async Task SendUpdateToCallCenter(OrderDto orderDto)
    {
        // Guard: Only sync orders that came from a Call Center.
        // Local orders (Takeaway, DineIn, local Delivery) have no CallCenterApiUrl and no CallCenterOrderId.
        if (string.IsNullOrEmpty(orderDto.CallCenterApiUrl) && !orderDto.CallCenterOrderId.HasValue)
        {
            _logger.LogDebug("Order {OrderId} has no CC relationship (no CallCenterApiUrl, no CallCenterOrderId). Skipping CC sync.", orderDto.OrderId);
            return;
        }

        var targetUrl = ResolveCallCenterApiUrl(orderDto.CallCenterApiUrl);
        if (string.IsNullOrEmpty(targetUrl))
        {
            _logger.LogDebug("Order {OrderId} does not have a valid CallCenterApiUrl. Skipping sync.", orderDto.OrderId);
            return;
        }
        orderDto.CallCenterApiUrl = targetUrl;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var json = System.Text.Json.JsonSerializer.Serialize(orderDto);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync($"{targetUrl}/api/order/receiveOrderUpdate", content);

            if (response.IsSuccessStatusCode)
            {
                PendingCallCenterSyncQueue.TryRemove(orderDto.Id);
                _logger.LogInformation("Order {OrderId} update sent to Call Center at {Url} successfully. State: {State}", 
                    orderDto.OrderId, targetUrl, orderDto.OrderState);
            }
            else
            {
                PendingCallCenterSyncQueue.Enqueue(orderDto);
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send order update to Call Center at {Url}. Status: {StatusCode}, Error: {Error}. Queued for retry.", 
                    targetUrl, response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            PendingCallCenterSyncQueue.Enqueue(orderDto);
            _logger.LogError(ex, "Exception while sending order update to Call Center at {Url} for order {OrderId}. Queued for retry.", targetUrl, orderDto.OrderId);
        }
    }

    [HttpGet("driver-settlement")]
    public async Task<ActionResult<List<DriverSettlementDto>>> GetDriverSettlement([FromQuery] DateTime posDate)
    {
        var settlement = await _distributionService.GetDriverSettlementAsync(posDate);
        return Ok(settlement);
    }

   

    [HttpGet("voided-orders")]
    public async Task<ActionResult<List<OrderDto>>> GetVoidedOrders([FromQuery] DateTime posDate)
    {
        var orders = await _distributionService.GetVoidedDeliveryOrdersAsync(posDate);
        return Ok(_mapper.Map<List<OrderDto>>(orders));
    }
}
