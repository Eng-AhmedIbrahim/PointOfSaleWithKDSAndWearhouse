using POS.Core.Services.Contract.VoidServices;
using Microsoft.AspNetCore.Diagnostics;

namespace POS.API.Controllers;

public class OrderController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IBranchService _branchService;
    private readonly IPrinterServices _printerServices;
    private readonly IKitchenServices _kitchenServices;
    private readonly IMapper _mapper;
    private readonly IDineInOrderService _dineInOrderService;
    private readonly string _reportsFolder;
    private readonly CallCenterSettings _callCenterSettings;
    private readonly IHubContext<DeliveryHub> _deliveryHubContext;
    private readonly IDeliveryZoneServices _deliveryZoneServices;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IVoidService _voidService;

    public OrderController(IOrderService orderService,
        IWebHostEnvironment webHostEnvironment,
        IBranchService branchService,
        IPrinterServices printerServices,
        IKitchenServices kitchenServices,
        IMapper mapper,
        IDineInOrderService dineInOrderService,
        CallCenterSettings callCenterSettings,
        IHubContext<DeliveryHub> deliveryHubContext,
        IDeliveryZoneServices deliveryZoneServices,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IVoidService voidService)
    {
        _orderService = orderService;
        _webHostEnvironment = webHostEnvironment;
        _branchService = branchService;
        _printerServices = printerServices;
        _kitchenServices = kitchenServices;
        _mapper = mapper;
        _dineInOrderService = dineInOrderService;
        _callCenterSettings = callCenterSettings;
        _deliveryHubContext = deliveryHubContext;
        _deliveryZoneServices = deliveryZoneServices;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _voidService = voidService;
        _reportsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports");
        Directory.CreateDirectory(_reportsFolder);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(Orders), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost("createOrder")]
    public async Task<IActionResult> CreateOrderAsync([FromBody] OrderDto orderDto)
    {
        if (orderDto == null || orderDto.OrderDetails == null || !orderDto.OrderDetails.Any())
            return BadRequest("Invalid order data.");

        var order = BackupMainOrderDetails(orderDto);

        if (Enum.TryParse<OrderTypes>(orderDto.OrderType ?? "TakeAway", out var orderType))
        {
            var orderSettings = orderDto.OrderSettings!.FirstOrDefault(o => o.OrderType == orderType.ToString());
            order.OrderType = orderType;

            if (orderType == OrderTypes.TakeAway || orderType == OrderTypes.Staff || orderType == OrderTypes.Hospitality)
            {
                BackupTakeawayOrder(orderDto, order);
                var createdOrder = await _orderService.CreateOrderAsync(order);

                if (createdOrder is null)
                    return BadRequest(new ApiResponse(404, "Order Not Created"));

                if (createdOrder!.Id != 0)
                {
                    if (orderDto.SkipPrintingOnServer != true)
                    {
                        if (orderType == OrderTypes.TakeAway || orderType == OrderTypes.Hospitality)
                        {
                            List<string> branchDetails = await GetBranchDetails(orderDto);
                            await printTakeAwayReceipts(orderDto, createdOrder, branchDetails);
                        }

                        if (orderSettings?.FullKitchenReceiptCount > 0)
                        {
                            await PrintBackupReceipts(orderDto, createdOrder);
                        }

                        if (orderSettings?.SeparateReceiptCount > 0)
                        {
                            await PrintKitchenReceipts(orderDto, createdOrder);
                        }
                    }

                    return Ok(_mapper.Map<OrderDto>(createdOrder));
                }
                else
                {
                    return BadRequest(new ApiResponse(404, "Order Not Created"));
                }
            }
            else if (orderType == OrderTypes.Delivery)
            {
                if (!_callCenterSettings.IsCentralCallCenter)
                {
                    orderDto.OrderState = OrderStates.Assigned.ToString();
                    order.OrderState = OrderStates.Assigned;
                }

                BackupDeliveryOrder(orderDto, order);

                // Fetch Zone info if missing (Bonus OR Fees)
                if (order.ZoneID.HasValue)
                {
                    var zone = await _deliveryZoneServices.GetZoneByIdAsync(order.ZoneID.Value);
                    if (zone != null)
                    {
                        if (order.ZoneBonus == null || order.ZoneBonus == 0)
                        {
                            order.ZoneBonus = zone.ZoneBonus;
                            orderDto.ZoneBonus = zone.ZoneBonus;
                        }

                        if ((order.DeliveryFees == null || order.DeliveryFees == 0) && order.WithoutDeliveryFees != true)
                        {
                            order.DeliveryFees = zone.DeliveryFee;
                            orderDto.DeliveryFees = zone.DeliveryFee;
                        }
                    }
                }

                var createdOrder = await _orderService.CreateOrderAsync(order);

                if (createdOrder is null || createdOrder.Id == 0)
                    return BadRequest(new ApiResponse(404, "Order Not Created"));

                orderDto.OrderId = createdOrder.OrderID;
                orderDto.Id = createdOrder.Id;

                if (_callCenterSettings.IsCentralCallCenter)
                {
                    var branchUrlToDispatch = orderDto.DeliveryBranchUrl ?? string.Empty;
                    Log.Information("[CC Dispatch] Starting dispatch process for Order {OrderId} (CC PK: {CCId}) for Branch {BranchName} to URL: {Url}", orderDto.OrderId, createdOrder.Id, orderDto.BranchName, branchUrlToDispatch);

                    if (!string.IsNullOrEmpty(branchUrlToDispatch))
                    {
                        var sanitizedUrl = branchUrlToDispatch.Trim().TrimEnd('/', '\\');

                        // Add Call Center API URL so branch can send updates back
                        var callCenterApiUrl = ResolveCallCenterApiUrl($"{Request.Scheme}://{Request.Host}");
                        orderDto.CallCenterApiUrl = callCenterApiUrl;
                        Log.Information("[CC Dispatch] Setting CallCenterApiUrl to {CCUrl}", callCenterApiUrl);

                        using var httpClient = new HttpClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(5); // Fast timeout for immediate response

                        // Reset state to Assigned before sending to branch
                        // so branch doesn't save it as FailedToDeliverToBranch

                        //orderDto.OrderState = OrderStates.Assigned.ToString();

                        var json = JsonSerializer.Serialize(orderDto);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        try
                        {
                            var targetEndpoint = $"{sanitizedUrl}/api/order/receiveDispatchedOrder";
                            Log.Information("[CC Dispatch] Sending POST to {Endpoint}", targetEndpoint);

                            var response = await httpClient.PostAsync(targetEndpoint, content);
                            Log.Information("[CC Dispatch] Branch responded with StatusCode: {StatusCode} for Order {OrderId}", response.StatusCode, orderDto.OrderId);

                            if (response.IsSuccessStatusCode)
                            {
                                var branchResponseJson = await response.Content.ReadAsStringAsync();
                                Log.Information("[CC Dispatch] Successfully received branch confirmation for Order {OrderId}", orderDto.OrderId);
                                
                                var branchOrderDto = JsonSerializer.Deserialize<OrderDto>(branchResponseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                
                                createdOrder.OrderState = OrderStates.SentToBranch;
                                if (branchOrderDto != null)
                                {
                                    createdOrder.CallCenterOrderId = branchOrderDto.Id; // Store Branch PK ID on Central side
                                    Log.Information("[CC Dispatch] Linked Order {CC_ID} with Branch PK ID {Branch_PK}", createdOrder.Id, branchOrderDto.Id);
                                }
                                await _orderService.UpdateOrderAsync(createdOrder);
                                Log.Information("[CC Dispatch] Order {CC_ID} updated to SentToBranch state.", createdOrder.Id);

                                orderDto.OrderState = OrderStates.SentToBranch.ToString();
                                if (branchOrderDto != null) orderDto.CallCenterOrderId = branchOrderDto.Id;

                                await NotifyAgentMachine("ReceiveDeliveryOrderSendSuccess", orderDto);
                            }
                            else
                            {
                                var errorContent = await response.Content.ReadAsStringAsync();
                                createdOrder.OrderState = OrderStates.FailedToDeliverToBranch;
                                await _orderService.UpdateOrderStatusAsync(createdOrder.Id, OrderStates.FailedToDeliverToBranch);

                                orderDto.OrderState = OrderStates.FailedToDeliverToBranch.ToString();
                                await NotifyAgentMachine("ReceiveDeliveryOrderUnsend", orderDto, errorContent);
                                Log.Warning("[CC Dispatch] Immediate dispatch failed for Order {OrderId}: {Error}. Saved as FailedToDeliverToBranch for background retry.", orderDto.OrderId, errorContent);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Exception dispatching order {OrderId}", orderDto.OrderId);
                            createdOrder.OrderState = OrderStates.FailedToDeliverToBranch;
                            await _orderService.UpdateOrderStatusAsync(createdOrder.Id, OrderStates.FailedToDeliverToBranch);

                            orderDto.OrderState = OrderStates.FailedToDeliverToBranch.ToString();
                            await NotifyAgentMachine("ReceiveDeliveryOrderUnsend", orderDto, ex.Message) ;
                        }
                    }
                    else
                    {
                        createdOrder.OrderState = OrderStates.FailedToDeliverToBranch;
                        await _orderService.UpdateOrderStatusAsync(createdOrder.Id, OrderStates.FailedToDeliverToBranch);
                        await NotifyAgentMachine("ReceiveDeliveryOrderUnsend", orderDto, "No branch URL provided.");
                        Log.Warning("[CC Dispatch] No delivery branch URL provided for Order {OrderId}. Saved as FailedToDeliverToBranch.", orderDto.OrderId);
                    } 

                    if(orderDto.SkipPrintingOnServer != true)
                    { 
                        var localSettings = await _orderService.GetOrderSettingsAsync(orderDto.MachineName);
                        var deliverySettings = localSettings?.FirstOrDefault(o => o.OrderType == OrderTypes.Delivery.ToString());
                        
                        if (deliverySettings != null && (deliverySettings.CustomerReceiptCount ?? 0) > 0)
                        {
                            List<string> branchDetails = await GetBranchDetails(orderDto);
                            var currentPrintCount = await _orderService.IncrementPrintCountAsync(createdOrder.Id);
                            bool isCopy = currentPrintCount > 1;
                            await printDeliveryReceipts(orderDto, createdOrder, branchDetails, isCopy: isCopy);
                        }
                    }

                    return Ok(_mapper.Map<OrderDto>(createdOrder));
                }
                else
                {
                    await NotifyAgentMachine("ReceiveNewDeliveryOrder", orderDto);
                    if (orderDto.SkipPrintingOnServer != true)
                    {
                        List<string> branchDetails = await GetBranchDetails(orderDto);
                        await printDeliveryReceipts(orderDto, createdOrder, branchDetails);
                         
                        if (orderSettings!.FullKitchenReceiptCount > 0)
                            await PrintBackupReceipts(orderDto, createdOrder);

                        if (orderSettings.SeparateReceiptCount > 0)
                            await PrintKitchenReceipts(orderDto, createdOrder);
                    }

                    return Ok(_mapper.Map<OrderDto>(createdOrder));
                }
            }
            else if (orderType == OrderTypes.DineIn)
            {
                BackupDineInOrder(orderDto, order);
                
                if (orderDto.SkipPrintingOnServer != true)
                {
                    List<string> branchDetails = await GetBranchDetails(orderDto);
                    await printDineInReceipts(orderDto, branchDetails);

                    if (orderSettings!.FullKitchenReceiptCount > 0)
                    {
                        await PrintBackupReceipts(orderDto, order, isFollowUp: false, KitchenType: "Backup");
                    }

                    if (orderSettings.SeparateReceiptCount > 0)
                    {
                        await PrintKitchenReceipts(orderDto, order);
                    }
                }
                return Ok(orderDto);
            }
        }

        return Ok();
    }

    [HttpPost("resendOrderToBranch/{orderId}")]
    public async Task<IActionResult> ResendOrderToBranchAsync(int orderId)
    {
        Log.Information("[CC Manual Resend] Starting manual resend for Order ID {OrderId}", orderId);
        
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null) return NotFound(new ApiResponse(404, "Order not found."));

        if (order.OrderType != OrderTypes.Delivery)
             return BadRequest(new ApiResponse(400, "Only delivery orders can be resent to branch."));

        var orderDto = _mapper.Map<OrderDto>(order);
        var branchUrlToDispatch = order.DeliveryBranchUrl;

        if (string.IsNullOrEmpty(branchUrlToDispatch))
        {
             Log.Information("[CC Manual Resend] DeliveryBranchUrl is empty on order, fetching from Branch table for BranchID {BranchID}", order.BranchID);
             var branch = await _branchService.GetBranchByIdAsync(order.BranchID)!;
             branchUrlToDispatch = branch?.ApiUrl;
        }

        if (string.IsNullOrEmpty(branchUrlToDispatch))
        {
             Log.Warning("[CC Manual Resend] No API URL found for Order {OrderId}", orderId);
             return BadRequest(new ApiResponse(400, "No branch API URL found for this order."));
        }

        var sanitizedUrl = branchUrlToDispatch.Trim().TrimEnd('/', '\\');

        var callCenterApiUrl = $"{Request.Scheme}://{Request.Host}";
        orderDto.CallCenterApiUrl = callCenterApiUrl;

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var json = JsonSerializer.Serialize(orderDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var targetEndpoint = $"{sanitizedUrl}/api/order/receiveDispatchedOrder";
            Log.Information("[CC Manual Resend] Sending POST to {Endpoint} for Order {OrderId}", targetEndpoint, orderId);

            var response = await httpClient.PostAsync(targetEndpoint, content);
            Log.Information("[CC Manual Resend] Branch responded with StatusCode: {StatusCode} for Order {OrderId}", response.StatusCode, orderId);

            if (response.IsSuccessStatusCode)
            {
                var branchResponseJson = await response.Content.ReadAsStringAsync();
                var branchOrderDto = JsonSerializer.Deserialize<OrderDto>(branchResponseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                order.OrderState = OrderStates.SentToBranch;
                if (branchOrderDto != null)
                {
                    order.CallCenterOrderId = branchOrderDto.Id;
                }
                await _orderService.UpdateOrderAsync(order);

                await NotifyAgentMachine("OrderDispatchedCentralNotification", orderDto);
                Log.Information("[CC Manual Resend] SUCCESS: Order {OrderId} resent and updated to SentToBranch.", orderId);
                return Ok(new { Message = "Order resent successfully." });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Warning("[CC Manual Resend] FAILED: Order {OrderId}. StatusCode: {StatusCode}. Error: {Error}", orderId, response.StatusCode, errorContent);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse(500, $"Branch API error: {errorContent}"));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[CC Manual Resend] EXCEPTION resending order {OrderId}", orderId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse(500, $"Exception: {ex.Message}"));
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(Orders), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPut("createOrder")] // Aligned with OrderSettingsService.UpdateOrderAsync
    public async Task<IActionResult> UpdateOrderAsync([FromBody] OrderDto orderDto)
    {
        if (orderDto == null || orderDto.Id == 0)
            return BadRequest("Invalid order data or Id.");

        var existingOrder = await _orderService.GetOrderByIdAsync(orderDto.Id);
        if (existingOrder == null)
            return NotFound(new ApiResponse(404, "Order not found."));

        if (existingOrder.OrderState == OrderStates.Dispatched || existingOrder.OrderState == OrderStates.Delivered)
        {
            return BadRequest(new ApiResponse(400, "لا يمكن تعديل الطلب لأنه خرج للتوصيل بالفعل"));
        }

        var order = BackupMainOrderDetails(orderDto);
        
        if (order.OrderType == OrderTypes.Delivery)
        {
            BackupDeliveryOrder(orderDto, order);

            // Fetch Zone info if missing (Bonus OR Fees)
            if (order.ZoneID.HasValue)
            {
                var zone = await _deliveryZoneServices.GetZoneByIdAsync(order.ZoneID.Value);
                if (zone != null)
                {
                    if (order.ZoneBonus == null || order.ZoneBonus == 0)
                    {
                        order.ZoneBonus = zone.ZoneBonus;
                        orderDto.ZoneBonus = zone.ZoneBonus;
                    }

                    if ((order.DeliveryFees == null || order.DeliveryFees == 0) && order.WithoutDeliveryFees != true)
                    {
                        order.DeliveryFees = zone.DeliveryFee;
                        orderDto.DeliveryFees = zone.DeliveryFee;
                    }
                }
            }
        }
        
        // Ensure the mapped order has the correct Id and preserves existing state and CC links
        order.Id = orderDto.Id;
        order.OrderState = existingOrder.OrderState;
        order.CallCenterOrderId = existingOrder.CallCenterOrderId ?? orderDto.CallCenterOrderId;
        order.CallCenterApiUrl = existingOrder.CallCenterApiUrl ?? orderDto.CallCenterApiUrl;
        order.RemoteOrderId = existingOrder.RemoteOrderId ?? orderDto.RemoteOrderId;
        order.DeliveryBranchUrl = existingOrder.DeliveryBranchUrl ?? orderDto.DeliveryBranchUrl;
        order.DriverID = existingOrder.DriverID;
        order.DriverName = existingOrder.DriverName;
        order.DispatchID = existingOrder.DispatchID;
        order.AssignTime = existingOrder.AssignTime;
        order.ClosingTime = existingOrder.ClosingTime;
        order.BackTime = existingOrder.BackTime;
        order.CollectorID = existingOrder.CollectorID;
        order.CollectorName = existingOrder.CollectorName;

        var updatedOrder = await _orderService.FullUpdateOrderAsync(order);

        if (updatedOrder == null)
            return NotFound(new ApiResponse(404, "Order not found."));

        // Sync update to Call Center (if at branch) or to Branch (if at Call Center)
        var updatedDto = _mapper.Map<OrderDto>(updatedOrder);
        updatedDto.CallCenterOrderId = existingOrder.CallCenterOrderId;
        updatedDto.CallCenterApiUrl = existingOrder.CallCenterApiUrl;
        updatedDto.RemoteOrderId = existingOrder.RemoteOrderId;

        if (_callCenterSettings.IsCentralCallCenter)
        {
            var branchUrl = updatedOrder.DeliveryBranchUrl ?? existingOrder.DeliveryBranchUrl;
            if (!string.IsNullOrEmpty(branchUrl))
            {
                await SendUpdateToBranch(updatedDto, branchUrl, "receiveUpdateFromCallCenter");
            }
        }
        else
        {
            await SendUpdateToCallCenter(updatedDto);
        }

        // Print receipts after update with "تابع" flag (Branch only - CC does not print)
        if (orderDto.SkipPrintingOnServer != true && !_callCenterSettings.IsCentralCallCenter)
        {
            var branchDevice = Environment.MachineName;
            var orderSettingsList = await _orderService.GetOrderSettingsAsync(branchDevice);
            if (orderSettingsList == null || !orderSettingsList.Any())
                orderSettingsList = await _orderService.GetOrderSettingsAsync(null);

            var orderSettings = orderSettingsList?.FirstOrDefault(o =>
                string.Equals(o.OrderType, orderDto.OrderType, StringComparison.OrdinalIgnoreCase));

            if (orderSettings != null)
            {
                orderDto.OrderSettings = _mapper.Map<IReadOnlyList<OrderSetting>, ICollection<OrderSettingToReturnDto>>(orderSettingsList);
                orderDto.MachineName = branchDevice;

                List<string> branchDetails = await GetBranchDetails(orderDto);
                if (string.Equals(orderDto.OrderType, OrderTypes.Delivery.ToString(), StringComparison.OrdinalIgnoreCase))
                    await printDeliveryReceipts(orderDto, updatedOrder, branchDetails, isFollowUp: true);
                else if (string.Equals(orderDto.OrderType, OrderTypes.TakeAway.ToString(), StringComparison.OrdinalIgnoreCase))
                    await printTakeAwayReceipts(orderDto, updatedOrder, branchDetails, isFollowUp: true);

                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceipts(orderDto, updatedOrder, isFollowUp: true);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceipts(orderDto, updatedOrder, isFollowUp: true);
            }
        }

        return Ok(updatedDto);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(Orders), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost("receiveDispatchedOrder")]
    public async Task<IActionResult> ReceiveDispatchedOrderAsync([FromBody] OrderDto orderDto)
    {
        Log.Information("[Branch Receive] Received new dispatched order {OrderId} from Call Center {CCUrl}", orderDto.OrderId, orderDto.CallCenterApiUrl);
        
        if (orderDto == null || orderDto.OrderDetails == null || !orderDto.OrderDetails.Any())
        {
            Log.Warning("[Branch Receive] Invalid order data received for Order {OrderId}", orderDto?.OrderId);
            return BadRequest("Invalid order data received.");
        }

        try 
        {
            // check if order already exists to prevent duplication on retries (only if valid CC Id > 0)
            if (orderDto.Id > 0)
            {
                var existingOrder = await _orderService.GetOrderByCallCenterIdAsync(orderDto.Id);
                if (existingOrder != null)
                {
                    Log.Information("[Branch Receive] Order {OrderId} (CC Id: {CCId}) already exists in branch, skipping creation.", orderDto.OrderId, orderDto.Id);
                    return Ok(_mapper.Map<OrderDto>(existingOrder));
                }
            }
            else
            {
                Log.Warning("[Branch Receive] Warning: Order {OrderId} received with CC Id <= 0 ({CCId}). Skipping duplicate check.", orderDto.OrderId, orderDto.Id);
            }

            var order = BackupMainOrderDetails(orderDto);
            order.Id = 0; // Reset to 0 so EF Core creates a new local Primary Key in Branch DB
            BackupDeliveryOrder(orderDto, order); 
            order.CallCenterOrderId = orderDto.Id; 
            order.RemoteOrderId = orderDto.OrderId; 
            order.CallCenterApiUrl = orderDto.CallCenterApiUrl;
            // In Branch: newly received delivery order is Assigned ("تم التعيين" للفرع، بانتظار تعيين طيار)
            order.OrderState = OrderStates.Assigned;
            Log.Information("[Branch Receive] Setting OrderState to Assigned for newly received Order {OrderId} in branch", orderDto.OrderId);

            // Validate that all items exist in the branch database to avoid FK violation errors
            if (order.OrderDetails != null)
            {
                foreach (var item in order.OrderDetails)
                {
                    if (!item.MenuSalesItemId.HasValue) continue;

                    var itemInDb = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(item.MenuSalesItemId.Value);
                    if (itemInDb != null)
                    {
                        item.OrderType = OrderTypes.Delivery;
                        // Always override kitchen assignment from branch local DB (CC may send wrong kitchen IDs)
                        if (itemInDb.KitchenTypeId.HasValue)
                            item.ItemKitchenTypeId = itemInDb.KitchenTypeId;
                        else
                            item.ItemKitchenTypeId = null; // clear CC's value if branch has no assignment
                        if (itemInDb.Category?.KitchenTypeId.HasValue == true)
                            item.CategoryKitchenTypeId = itemInDb.Category.KitchenTypeId;
                        else
                            item.CategoryKitchenTypeId = null;
                        if (!item.PrintInBackupReceiptFromItem.HasValue)
                            item.PrintInBackupReceiptFromItem = itemInDb.PrintInBackupReceipt;
                        if (!item.PrintInBackupReceiptFromCategory.HasValue && itemInDb.Category != null)
                            item.PrintInBackupReceiptFromCategory = itemInDb.Category.PrintInBackupReceipt;
                    }
                    if (itemInDb == null)
                    {
                        var itemName = orderDto.OrderDetails.FirstOrDefault(d => d.Id == item.MenuSalesItemId.Value)?.Name ?? "Unknown Item";
                        Log.Error("[Branch Receive] Item {ItemName} (ID: {ItemId}) not found in branch DB. Aborting order creation.", itemName, item.MenuSalesItemId);
                        return BadRequest(new ApiResponse(400, $"الصنف '{itemName}' غير موجود في قاعدة بيانات الفرع. يرجى مراجعة الأصناف ومزامنتها."));
                    }
                }
            }

            Log.Information("[Branch Receive] Attempting to save order {OrderId} to local database...", orderDto.OrderId);
            var createdOrder = await _orderService.CreateOrderAsync(order);

            if (createdOrder is null || createdOrder.Id == 0)
            {
                Log.Error("[Branch Receive] FAILED to create dispatched order in branch local database. Order ID: {OrderId}", orderDto.OrderId);
                return BadRequest(new ApiResponse(404, "Dispatched Order Not Created in branch DB."));
            }

            Log.Information("[Branch Receive] Order {OrderId} saved successfully with local ID {LocalId}", orderDto.OrderId, createdOrder.Id);

            var branchOrderDto = _mapper.Map<OrderDto>(createdOrder);
            branchOrderDto.CallCenterOrderId = orderDto.Id;
            branchOrderDto.CallCenterApiUrl = orderDto.CallCenterApiUrl;
            branchOrderDto.DeliveryBranchUrl = orderDto.DeliveryBranchUrl;
            // Map OrderDetails from createdOrder so it carries branch enriched kitchen types & backup flags
            branchOrderDto.OrderDetails = _mapper.Map<List<TableItem>>(createdOrder.OrderDetails);

            await _deliveryHubContext.Clients.All.SendAsync("ReceiveNewDeliveryOrder", branchOrderDto);

            Log.Information("[Branch Receive] Completed processing for Order {OrderId}. Order broadcasted to Dispatcher via SignalR.", orderDto.OrderId);


            return Ok(branchOrderDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Branch Receive] EXCEPTION processing order {OrderId}", orderDto.OrderId);
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPut("receiveOrderUpdate")]
    public async Task<IActionResult> ReceiveOrderUpdateFromBranch([FromBody] OrderDto orderDto)
    {
        Log.Information("[CC Sync] Received order update from Branch for Order {OrderId} (BranchLocalId={BranchId}), CallCenterOrderId={CCId}, ParentOrderId={ParentId}, State: {State}",
            orderDto?.OrderId, orderDto?.Id, orderDto?.CallCenterOrderId, orderDto?.ParentOrderId, orderDto?.OrderState);
        
        if (orderDto == null || ((!orderDto.CallCenterOrderId.HasValue || orderDto.CallCenterOrderId <= 0) && !orderDto.ParentOrderId.HasValue))
        {
            Log.Warning("[CC Sync] Rejected update: missing CallCenterOrderId and ParentOrderId. OrderId={OrderId}", orderDto?.OrderId);
            return BadRequest("Invalid order update data: CallCenterOrderId or ParentOrderId is required.");
        }

        try 
        {
            Orders? order = null;
            if (orderDto.CallCenterOrderId.HasValue && orderDto.CallCenterOrderId.Value > 0)
            {
                order = await _orderService.GetOrderByIdAsync(orderDto.CallCenterOrderId.Value);
            }

            // Fallback: If not found by CallCenterOrderId PK, try matching by OrderID (Order Number) and BranchID
            if (order == null && orderDto.OrderId > 0 && orderDto.BranchId > 0)
            {
                Log.Warning("[CC Sync] Order not found by PK {CCId}. Attempting fallback lookup by OrderID={OrderId} and BranchId={BranchId}", 
                    orderDto.CallCenterOrderId, orderDto.OrderId, orderDto.BranchId);
                var spec = new BaseSpecifications<Orders>(o => o.OrderID == orderDto.OrderId && o.BranchID == orderDto.BranchId);
                order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
                if (order != null)
                {
                    Log.Information("[CC Sync] Fallback matched Order with PK {PK} for OrderID={OrderId}", order.Id, order.OrderID);
                }
            }

            if (order == null)
            {
                // If it has a ParentOrderId, it is a new addition from the branch
                if (orderDto.ParentOrderId.HasValue)
                {
                    Log.Information("Received new addition order from branch for Parent Order {ParentId}", orderDto.ParentOrderId);
                    var additionOrder = _mapper.Map<OrderDto, Orders>(orderDto);
                    additionOrder.OrderType = OrderTypes.Delivery;
                    additionOrder.CallCenterOrderId = orderDto.Id; 
                    
                    // Link to the CC's parent order if possible
                    var parentOrderCC = await _orderService.GetOrderByCallCenterIdAsync(orderDto.ParentOrderId.Value);
                    if (parentOrderCC != null)
                    {
                        additionOrder.ParentOrderId = parentOrderCC.Id;
                    }
                    
                    var createdAddition = await _orderService.CreateOrderAsync(additionOrder);
                    var returnDto = _mapper.Map<OrderDto>(createdAddition);
                    await _deliveryHubContext.Clients.All.SendAsync("ReceiveNewDeliveryOrder", returnDto);
                    return Ok(returnDto);
                }

                Log.Warning("Order not found in call center database. CallCenterOrderId: {OrderId}", orderDto.CallCenterOrderId);
                return NotFound(new ApiResponse(404, "Order not found in call center database."));
            }

            // If the order from branch has no driver (and not completed/voided), CC shows SentToBranch ("تم الإرسال للفرع")
            if (string.IsNullOrEmpty(orderDto.DriverID) 
                && orderDto.OrderState != OrderStates.Completed.ToString() 
                && orderDto.OrderState != OrderStates.Voided.ToString())
            {
                order.OrderState = OrderStates.SentToBranch;
                order.DriverID = null;
                order.DriverName = null;
                order.AssignTime = null;
                order.DispatchID = null;
            }
            else
            {
                order.OrderState = Enum.TryParse<OrderStates>(orderDto.OrderState, out var state) ? state : order.OrderState;
                order.DriverID = orderDto.DriverID;
                order.DriverName = orderDto.DriverName;
                order.AssignTime = orderDto.AssignTime;
                order.DispatchID = orderDto.DispatchID;
            }
            order.ClosingTime = orderDto.ClosingTime ?? order.ClosingTime;
            order.CollectorID = orderDto.CollectorID ?? order.CollectorID;
            order.CollectorName = orderDto.CollectorName ?? order.CollectorName;
            order.BackTime = orderDto.BackTime ?? order.BackTime;
            if (orderDto.GrandTotal.HasValue && orderDto.GrandTotal > 0) order.GrandTotal = orderDto.GrandTotal.Value;
            if (orderDto.SubTotal.HasValue && orderDto.SubTotal > 0) order.Subtotal = orderDto.SubTotal.Value;
            
            // Link the Branch's primary key and sequential order number to the Central record 
            order.CallCenterOrderId = orderDto.Id; 
            order.RemoteOrderId = orderDto.OrderId; 
            
            // Sync Voiding details
            if (order.OrderState == OrderStates.Voided)
            {
                order.VoidBy = orderDto.VoidBy;
                order.VoidByName = orderDto.VoidByName;
                order.VoidReason = orderDto.VoidReason;
                order.VoidTime = orderDto.VoidTime ?? DateTime.Now;
                order.VoidAmount = orderDto.VoidAmount ?? order.VoidAmount;
                order.TotalVoid = orderDto.TotalVoid ?? order.TotalVoid;
                order.VoidCount = orderDto.VoidCount ?? order.VoidCount;
            }

            if (orderDto.OrderDetails != null && orderDto.OrderDetails.Any())
            {
                var orderItems = _mapper.Map<List<TableItem>, List<OrderItemsDetails>>(orderDto.OrderDetails);
                var effectiveOrderType = order.OrderType ?? (Enum.TryParse<OrderTypes>(orderDto.OrderType, out var pt1) ? pt1 : OrderTypes.Delivery);

            foreach (var oi in orderItems)
            {
                oi.OrderType = effectiveOrderType;
                if (oi.MenuSalesItemId.HasValue)
                {
                    try
                    {
                        var localItem = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(oi.MenuSalesItemId.Value);
                        if (localItem != null)
                        {
                            // Always override kitchen assignment from branch local DB (CC may send wrong kitchen IDs)
                            oi.ItemKitchenTypeId = localItem.KitchenTypeId;
                            oi.CategoryKitchenTypeId = localItem.Category?.KitchenTypeId;
                            if (!oi.PrintInBackupReceiptFromItem.HasValue)
                                oi.PrintInBackupReceiptFromItem = localItem.PrintInBackupReceipt;
                            if (!oi.PrintInBackupReceiptFromCategory.HasValue && localItem.Category != null)
                                oi.PrintInBackupReceiptFromCategory = localItem.Category.PrintInBackupReceipt;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "[ReceiveUpdateFromCallCenter] Could not enrich item {Id}", oi.MenuSalesItemId);
                    }
                }
            }

            order.OrderDetails = orderItems;
                order.Tax = orderDto.Tax ?? order.Tax;
                order.TotalDiscount = orderDto.TotalOrderDiscount ?? order.TotalDiscount;
                order.Discount = orderDto.DiscountPercentage ?? order.Discount;
                await _orderService.FullUpdateOrderAsync(order);
            }
            else
            {
                await _orderService.UpdateOrderAsync(order);
            }

            // Record in OrderVoid table for Void Reports in Call Center if order was voided
            if (order.OrderState == OrderStates.Voided)
            {
                try
                {
                    var existingVoidSpec = new BaseSpecifications<OrderVoid>(v => v.OrderId == order.Id);
                    var existingVoids = await _unitOfWork.Repository<OrderVoid>().GetAllWithSpecificationAsync(existingVoidSpec);
                    if (!existingVoids.Any())
                    {
                        var appDates = await _unitOfWork.Repository<AppDate>().GetAllAsync();
                        var appDate = appDates?.FirstOrDefault();
                        DateTime workingDate = (appDate != null && appDate.PosDate.Year > 2000) ? appDate.PosDate.Date : DateTime.Today;
                        DateTime effVoidTime = order.VoidTime ?? workingDate.Add(DateTime.Now.TimeOfDay);

                        var voidOp = new OrderVoid
                        {
                            OrderId = order.Id,
                            OrderType = order.OrderType ?? OrderTypes.Delivery,
                            OrderStateAtVoid = OrderStates.Assigned,
                            VoidDate = effVoidTime,
                            VoidedBy = order.VoidBy ?? "System",
                            VoidedByName = order.VoidByName ?? "الفرع",
                            Reason = order.VoidReason ?? "إلغاء من الفرع",
                            IsFullVoid = true,
                            SubtotalBefore = order.Subtotal ?? 0,
                            TaxBefore = order.Tax ?? 0,
                            ServiceBefore = order.Service ?? 0,
                            DeliveryFeesBefore = order.DeliveryFees ?? 0,
                            DiscountBefore = order.Discount ?? 0,
                            GrandTotalBefore = (order.TotalVoid.HasValue && order.TotalVoid > 0) ? order.TotalVoid.Value : (order.GrandTotal ?? 0),
                            TotalVoidedAmount = (order.TotalVoid.HasValue && order.TotalVoid > 0) ? order.TotalVoid.Value : (order.GrandTotal ?? 0)
                        };

                        if (order.OrderDetails != null && order.OrderDetails.Any())
                        {
                            foreach (var d in order.OrderDetails)
                            {
                                voidOp.VoidItems.Add(new OrderVoidItem
                                {
                                    OrderDetailId = d.Id,
                                    QuantityBefore = d.Quantity ?? 1,
                                    QuantityVoided = d.Quantity ?? 1,
                                    QuantityAfter = 0,
                                    AmountBefore = d.TotalAmount ?? 0,
                                    AmountVoided = d.TotalAmount ?? 0,
                                    AmountAfter = 0,
                                    Reason = order.VoidReason ?? "إلغاء من الفرع"
                                });
                            }
                        }

                        await _unitOfWork.Repository<OrderVoid>().AddAsync(voidOp);
                        await _unitOfWork.CompleteAsync();
                        Log.Information("[CC Sync] Created OrderVoid record for order {OrderId} in CC database", order.OrderID);
                    }
                }
                catch (Exception voidEx)
                {
                    Log.Error(voidEx, "[CC Sync] Error creating OrderVoid history record for order {OrderId}", order.OrderID);
                }
            }

            Log.Information("Order {OrderId} updated in call center from branch. State: {State}", order.OrderID, order.OrderState);

            // Notify CC UI
            await _deliveryHubContext.Clients.All.SendAsync("ReceiveOrderUpdated", _mapper.Map<OrderDto>(order));

            return Ok(_mapper.Map<OrderDto>(order));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[CC Sync] EXCEPTION in ReceiveOrderUpdateFromBranch");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("receiveUpdateFromCallCenter")]
    public async Task<IActionResult> ReceiveUpdateFromCallCenter([FromBody] OrderDto orderDto)
    {
        if (orderDto == null || orderDto.Id <= 0)
            return BadRequest("Invalid order update data.");

        // On branch side, we find order where CallCenterOrderId matches the incoming Id (which is CC's PK)
        var order = await _orderService.GetOrderByCallCenterIdAsync(orderDto.Id);

        if (order == null)
        {
            Log.Warning("Order {OrderId} not found in branch database for update from Call Center.", orderDto.OrderId);
            return NotFound(new ApiResponse(404, "Order not found in branch database."));
        }

        bool wasAlreadyVoided = order.OrderState == OrderStates.Voided;

        // If this is a FULL VOID from Call Center:
        if (orderDto.OrderState == OrderStates.Voided.ToString())
        {
            if (!wasAlreadyVoided)
            {
                await _voidService.VoidOrderAsync(
                    order.Id,
                    orderDto.VoidReason ?? "Ø¥Ù„ØºØ§Ø¡ Ù…Ù† Ø§Ù„ÙƒÙˆÙ„ Ø³Ù†ØªØ±",
                    orderDto.VoidBy ?? "CC",
                    orderDto.VoidByName ?? "Ø§Ù„ÙƒÙˆÙ„ Ø³Ù†ØªØ±",
                    returnToStock: true);

                order = await _orderService.GetOrderByIdAsync(order.Id) ?? order;
            }

            var resultDto = _mapper.Map<OrderDto>(order);
            var orderSettings = await _orderService.GetOrderSettingsAsync(null);
            resultDto.OrderSettings = _mapper.Map<IReadOnlyList<OrderSetting>, ICollection<OrderSettingToReturnDto>>(orderSettings);

            var settingForType = orderSettings?.FirstOrDefault(s =>
                string.Equals(s.OrderType, order.OrderType.ToString(), StringComparison.OrdinalIgnoreCase));

            // Void printing is handled by Desktop UI (DesktopPrintOrderService via ReceiveOrderUpdated)

            // Notify branch UI
            await _deliveryHubContext.Clients.All.SendAsync("ReceiveOrderUpdated", resultDto);

            return Ok(resultDto);
        }

        // If this is a PARTIAL item void from Call Center:
        var ccVoidedItems = orderDto.OrderDetails?.Where(i => i.IsVoided == true || (i.VoidAmount.HasValue && i.VoidAmount > 0)).ToList();
        if (ccVoidedItems != null && ccVoidedItems.Any())
        {
            var itemsToVoidAtBranch = new List<OrderItemVoidDto>();
            foreach (var ccItem in ccVoidedItems)
            {
                var branchDetail = order.OrderDetails?.FirstOrDefault(d =>
                    d.MenuSalesItemId.HasValue && d.MenuSalesItemId == ccItem.Id && d.IsVoided != true);

                if (branchDetail != null)
                {
                    decimal qtyToVoid = (ccItem.VoidAmount.HasValue && ccItem.VoidAmount > 0)
                        ? ccItem.VoidAmount.Value
                        : (branchDetail.Quantity ?? 0);

                    itemsToVoidAtBranch.Add(new OrderItemVoidDto(branchDetail.Id, qtyToVoid));
                }
            }

            if (itemsToVoidAtBranch.Any())
            {
                await _voidService.VoidItemsAsync(
                    order.Id,
                    itemsToVoidAtBranch,
                    orderDto.VoidReason ?? "Ø¥Ù„ØºØ§Ø¡ ØµÙ†Ù Ù…Ù† Ø§Ù„ÙƒÙˆÙ„ Ø³Ù†ØªØ±",
                    orderDto.VoidBy ?? "CC",
                    orderDto.VoidByName ?? "Ø§Ù„ÙƒÙˆÙ„ Ø³Ù†ØªØ±",
                    returnToStock: true);

                order = await _orderService.GetOrderByIdAsync(order.Id) ?? order;

                try
                {
                    var resultDto = _mapper.Map<OrderDto>(order);
                    var orderSettings = await _orderService.GetOrderSettingsAsync(null);
                    resultDto.OrderSettings = _mapper.Map<IReadOnlyList<OrderSetting>, ICollection<OrderSettingToReturnDto>>(orderSettings);

                    // Void printing is handled by Desktop UI (DesktopPrintOrderService via ReceiveOrderUpdated)
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[CC Item Void Sync] Error printing receipts for partial void of order {OrderId} at branch", order.OrderID);
                }
            }
        }

        // Update basic tracking fields
        order.OrderState = Enum.TryParse<OrderStates>(orderDto.OrderState, out var state) ? state : order.OrderState;
        order.DriverID = orderDto.DriverID ?? order.DriverID;
        order.DriverName = orderDto.DriverName ?? order.DriverName;
        order.AssignTime = orderDto.AssignTime ?? order.AssignTime;
        order.DispatchID = orderDto.DispatchID ?? order.DispatchID;
        order.ClosingTime = orderDto.ClosingTime ?? order.ClosingTime;

        if (orderDto.GrandTotal.HasValue && orderDto.GrandTotal > 0) order.GrandTotal = orderDto.GrandTotal.Value;
        if (orderDto.SubTotal.HasValue && orderDto.SubTotal > 0) order.Subtotal = orderDto.SubTotal.Value;

        bool hasItemChanges = orderDto.OrderDetails != null && orderDto.OrderDetails.Any(d => d.IsVoided != true);
        var addedItems = new List<TableItem>();

        if (hasItemChanges)
        {
            var existingItemMap = order.OrderDetails?
                .GroupBy(d => d.MenuSalesItemId ?? 0)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity ?? 0))
                ?? new Dictionary<int, decimal>();

            var orderItems = _mapper.Map<List<TableItem>, List<OrderItemsDetails>>(orderDto.OrderDetails!);
            var effectiveOrderType = order.OrderType ?? (Enum.TryParse<OrderTypes>(orderDto.OrderType, out var pt2) ? pt2 : OrderTypes.Delivery);

            foreach (var oi in orderItems)
            {
                oi.OrderType = effectiveOrderType;
                if (oi.MenuSalesItemId.HasValue)
                {
                    try
                    {
                        var localItem = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(oi.MenuSalesItemId.Value);
                        if (localItem != null)
                        {
                            // Always override kitchen assignment from branch local DB (CC may send wrong kitchen IDs)
                            oi.ItemKitchenTypeId = localItem.KitchenTypeId;
                            oi.CategoryKitchenTypeId = localItem.Category?.KitchenTypeId;
                            if (!oi.PrintInBackupReceiptFromItem.HasValue)
                                oi.PrintInBackupReceiptFromItem = localItem.PrintInBackupReceipt;
                            if (!oi.PrintInBackupReceiptFromCategory.HasValue && localItem.Category != null)
                                oi.PrintInBackupReceiptFromCategory = localItem.Category.PrintInBackupReceipt;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "[ReceiveUpdateFromCallCenter] Could not enrich item {Id}", oi.MenuSalesItemId);
                    }
                }
            }

            order.OrderDetails = orderItems;
            order.Tax = orderDto.Tax ?? order.Tax;
            order.TotalDiscount = orderDto.TotalOrderDiscount ?? order.TotalDiscount;
            order.Discount = orderDto.DiscountPercentage ?? order.Discount;
            await _orderService.FullUpdateOrderAsync(order);

            addedItems = new List<TableItem>();
            foreach (var item in orderDto.OrderDetails!)
            {
                var existingQty = existingItemMap.TryGetValue(item.Id, out var eq) ? eq : 0m;
                var incomingQty = item.Quantity;
                if (incomingQty > existingQty)
                {
                    var addedItem = item.Clone();
                    addedItem.Quantity = incomingQty - existingQty;
                    addedItem.Total = addedItem.Quantity * (item.Price ?? 0);
                    addedItem.TotalAmount = addedItem.Quantity * (item.Price ?? 0);
                    addedItems.Add(addedItem);
                }
            }

            foreach (var ai in addedItems)
            {
                try
                {
                    var localItem = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(ai.Id);
                    if (localItem != null)
                    {
                        ai.ItemKitchenTypeId = localItem.KitchenTypeId;
                        ai.CategoryKitchenTypeId = localItem.Category?.KitchenTypeId;
                        ai.PrintInBackupReceiptFromItem = localItem.PrintInBackupReceipt;
                        if (localItem.Category != null)
                            ai.PrintInBackupReceiptFromCategory = localItem.Category.PrintInBackupReceipt;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[ReceiveUpdateFromCallCenter] Could not enrich added item {Id}", ai.Id);
                }
            }
            // Printing is handled by POS.Desktop locally via ReceiveOrderUpdated SignalR event
        }
        else
        {
            await _orderService.UpdateOrderAsync(order);
        }

        var updatedNotifyDto = _mapper.Map<OrderDto>(order);
        updatedNotifyDto.CallCenterOrderId = order.CallCenterOrderId;
        if (order.OrderDetails != null && order.OrderDetails.Any())
        {
            updatedNotifyDto.OrderDetails = _mapper.Map<List<OrderItemsDetails>, List<TableItem>>(order.OrderDetails.ToList());
        }

        if (ccVoidedItems != null && ccVoidedItems.Any())
        {
            updatedNotifyDto.VoidedItems = ccVoidedItems;
            foreach (var vi in updatedNotifyDto.VoidedItems)
            {
                try
                {
                    var localItem = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(vi.Id);
                    if (localItem != null)
                    {
                        vi.ItemKitchenTypeId = localItem.KitchenTypeId;
                        vi.CategoryKitchenTypeId = localItem.Category?.KitchenTypeId;
                        vi.PrintInBackupReceiptFromItem = localItem.PrintInBackupReceipt;
                        if (localItem.Category != null)
                            vi.PrintInBackupReceiptFromCategory = localItem.Category.PrintInBackupReceipt;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[ReceiveUpdateFromCallCenter] Could not enrich voided item {Id}", vi.Id);
                }
            }
        }

        if (hasItemChanges)
        {
            if (updatedNotifyDto.OrderDetails == null || !updatedNotifyDto.OrderDetails.Any())
                updatedNotifyDto.OrderDetails = orderDto.OrderDetails;

            if (addedItems.Count > 0)
            {
                updatedNotifyDto.IsFollowUp = true;
                updatedNotifyDto.AddedItems = addedItems;
            }
            else
            {
                updatedNotifyDto.IsFollowUp = false;
            }
        }
        await _deliveryHubContext.Clients.All.SendAsync("ReceiveOrderUpdated", updatedNotifyDto);

        return Ok(true);
    }

    private async Task PrintVoidToKitchen(OrderDto orderDto, List<OrderItemsDetails>? itemsToVoid)
    {
        try 
        {
            if (itemsToVoid == null || !itemsToVoid.Any()) return;

            var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
            var kitchenItems = _mapper.Map<List<OrderItemsDetails>, List<TableItem>>(itemsToVoid);

            // Always override kitchen assignment from branch local DB (CC may send wrong kitchen IDs)
            foreach (var item in kitchenItems)
            {
                if (item.Id > 0)
                {
                    try
                    {
                        var localItem = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(item.Id);
                        if (localItem != null)
                        {
                            item.ItemKitchenTypeId = localItem.KitchenTypeId;
                            item.CategoryKitchenTypeId = localItem.Category?.KitchenTypeId;
                            if (localItem.PrintInBackupReceipt.HasValue) item.PrintInBackupReceiptFromItem = localItem.PrintInBackupReceipt;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "[PrintVoidToKitchen] Could not fetch local KitchenTypeId for MenuSalesItemId={Id}", item.Id);
                    }
                }
            }

            var groupedItems = kitchenItems
                .Where(item => item.ItemKitchenTypeId.HasValue || item.CategoryKitchenTypeId.HasValue)
                .GroupBy(item => item.ItemKitchenTypeId ?? item.CategoryKitchenTypeId!.Value)
                .Select(g => 
                {
                    var kitchen = kitchens.FirstOrDefault(k => k.Id == g.Key);
                    var printers = kitchen?.KitchenPrinters?
                        .FirstOrDefault(p => p.DeviceName == Environment.MachineName) ?? kitchen?.KitchenPrinters?.FirstOrDefault();
                    
                    return new
                    {
                        KitchenName = kitchen?.KitchenName ?? $"Kitchen_{g.Key}",
                        Items = g.ToList(),
                        Printers = printers
                    };
                })
                .Where(x => x.Printers != null);

            foreach (var group in groupedItems)
            {
                var receipt = new KitchenReceipt()
                {
                    Id = orderDto.OrderId,
                    CashierName = orderDto.CashierName ?? "System",
                    OrderType = orderDto.OrderType ?? "Unknown",
                    DateCreated = DateTimeOffset.Now,
                    Items = group.Items,
                    KitchenNote = $"*** VOID FROM CC/ملغي من الكول سنتر ***\nReason: {orderDto.VoidReason}",
                    KitchenType = group.KitchenName,
                    TableId = orderDto.TableId,
                    TableName = orderDto.TableName,
                    IsVoid = true
                };

                var document = new KitchenReceiptDocument(receipt, group.Items);
                var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
                var outputPath = Path.Combine(_reportsFolder, $"VOID_SYNC_{timestamp}_{receipt.Id}.pdf");
                document.GeneratePdf(outputPath);

                var printers = group.Printers;
                if (printers == null) continue;
                for (int i = 1; i <= 3; i++)
                {
                    var printerName = printers.GetType().GetProperty($"Copy{i}")?.GetValue(printers) as string;
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        await _printerServices.PrintPdfAsync(outputPath, printerName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error printing void sync to kitchen for order {OrderId}", orderDto.OrderId);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrderByIdAsync(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound();
        return Ok(_mapper.Map<OrderDto>(order));
    }


    
    [HttpPut("incrementPrintCount/{id}")]
    public async Task<ActionResult<int>> IncrementPrintCount(int id)
    {
        try
        {
            var result = await _orderService.IncrementPrintCountAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for order {Id}", id);
            return StatusCode(500, new ApiResponse(500, "Internal server error"));
        }
    }
    


    private async Task printDineInReceipts(OrderDto dineInOrder, List<string> branchDetails)
    {
        // Try to find the order to check print count
        var currentPrintCount = await _dineInOrderService.IncrementPrintCountAsync(dineInOrder.Id);
        bool isCopy = currentPrintCount > 0;

        var receipt = new Receipt()
        {
            Id = dineInOrder.OrderId,
            StoreName = dineInOrder.BranchName!,
            CashierName = dineInOrder.CashierName!,
            ReceiptType = OrderTypes.DineIn.ToString(),
            DateCreated = DateTime.Now,
            PaymentMethod = dineInOrder.PaymentMethod?.ToString() ?? "N/A",
            FooterMessage = dineInOrder.FooterMessage ?? "",
            LogoPath = branchDetails[0],
            LogoWidth = int.Parse(branchDetails[1]),
            TotalAmount = dineInOrder.GrandTotal,
            Discount = dineInOrder.TotalOrderDiscount,
            Tax = dineInOrder.Tax,
            Services = dineInOrder.Services,
            SubTotal = dineInOrder.SubTotal,
            Items = dineInOrder.OrderDetails!,
            TableId = dineInOrder.TableId,
            TableName = dineInOrder.TableName,
            WaiterName = dineInOrder.WaiterName,
            IsCopy = isCopy,
            IsVoid = dineInOrder.OrderState == OrderStates.Voided.ToString() || (dineInOrder.VoidAmount.HasValue && dineInOrder.VoidAmount > 0)
        };

        var outputPath = await CreateCashReceiptLayOut(receipt, receipt.Items);
        await PrintDineInReceipt(dineInOrder, outputPath);
    }

    private async Task PrintDineInReceipt(OrderDto dineInOrder, string reportPath)
    {
        var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
        var printer = kitchens?.ElementAtOrDefault(0);

        var receiptCount = dineInOrder.OrderSettings!
                .Where(o => o.OrderType == OrderTypes.DineIn.ToString())
                .Select(o => o.CustomerReceiptCount)
                .FirstOrDefault();

        if (printer == null || receiptCount <= 0)
            return;

        var targetPrinter = printer?.KitchenPrinters?.FirstOrDefault(p => p.DeviceName == (dineInOrder.MachineName ?? Environment.MachineName))
                         ?? printer?.KitchenPrinters?.FirstOrDefault();
        if (targetPrinter == null) return;
        var printerNamesToUse = new List<string>();

        for (int i = 1; i <= receiptCount; i++)
        {
            var printerName = targetPrinter.GetType().GetProperty($"Copy{i}")?.GetValue(targetPrinter) as string;
            if (!string.IsNullOrEmpty(printerName))
            {
                printerNamesToUse.Add(printerName);
            }
        }

        foreach (var printerName in printerNamesToUse)
            await _printerServices.PrintPdfAsync(reportPath, printerName);
    }
    private async Task<List<string>> GetBranchDetails(OrderDto orderDto)
    {
        List<string> branchDetails = new();

        if (branchDetails.Count <= 0)
            branchDetails = await GetLogoPath(orderDto.BranchId);
        return branchDetails;
    }
    private static Orders BackupMainOrderDetails(OrderDto OrderDto)
    {
        Enum.TryParse<OrderTypes>(OrderDto.OrderType ?? "TakeAway", out var orderType);
        
        return new Orders
        {
            Id = OrderDto.Id,
            OrderID = OrderDto.OrderId,
            BranchID = OrderDto.BranchId,
            BranchName = OrderDto.BranchName,
            CashierID = OrderDto.CashierId,
            CashierName = OrderDto.CashierName,
            OrderDate = OrderDto.OrderDate,
            PaymentMethod = OrderDto.PaymentMethod,
            OrderState = DetermineInitialState(OrderDto, orderType),
            Discount = (OrderDto.TotalDiscount.HasValue && OrderDto.TotalDiscount.Value > 0)
                ? OrderDto.TotalDiscount.Value
                : ((OrderDto.TotalOrderDiscount.HasValue && OrderDto.TotalOrderDiscount.Value > 0)
                    ? OrderDto.TotalOrderDiscount.Value
                    : (OrderDto.DiscountedItems ?? 0)),
            DiscountByName = OrderDto.DiscountByName,
            DiscountReason = OrderDto.DiscountReason,
            DiscountType = OrderDto.DiscountType,
            DiscountPercentage = OrderDto.DiscountPercentage,
            TotalDiscount = (OrderDto.TotalDiscount.HasValue && OrderDto.TotalDiscount.Value > 0)
                ? OrderDto.TotalDiscount.Value
                : ((OrderDto.TotalOrderDiscount.HasValue && OrderDto.TotalOrderDiscount.Value > 0)
                    ? OrderDto.TotalOrderDiscount.Value
                    : (OrderDto.DiscountedItems ?? 0)),
            TotalVoid = OrderDto.TotalVoid,
            VoidCount = OrderDto.VoidCount,
            VoidAmount = OrderDto.VoidAmount,
            Paid = OrderDto.Paid,
            Remain = OrderDto.Remaining,  // ✅ إضافة حفظ المبلغ المتبقي
            Subtotal = OrderDto.SubTotal,
            Service = OrderDto.Services,
            Tax = OrderDto.Tax,
            GrandTotal = OrderDto.GrandTotal,
            OrderNotice = OrderDto.OrderNotice,
            MachineName = OrderDto.MachineName,
            TableID = OrderDto.TableId,
            TableName = OrderDto.TableName,
            OrderType = orderType,
            CallCenterOrderId = OrderDto.CallCenterOrderId,
            RemoteOrderId = OrderDto.RemoteOrderId,
            IsHospitality = OrderDto.IsHospitality,
            HospitalityResponsibleId = OrderDto.HospitalityResponsibleId,
            HospitalityResponsibleName = OrderDto.HospitalityResponsibleName,
            HospitalityReason = OrderDto.HospitalityReason,
            IsStaffMeal = OrderDto.IsStaffMeal,
            StaffMealEmployeeId = OrderDto.StaffMealEmployeeId,
            StaffMealEmployeeName = OrderDto.StaffMealEmployeeName,
            OrderDetails = OrderDto.OrderDetails?.Select(detail => new OrderItemsDetails
            {
                MenuSalesItemId = detail.Id,
                Quantity = detail.Quantity,
                TotalAmount = detail.TotalAmount,
                Discount = detail.HasDiscount,
                TotalDiscountPercentage = detail.DiscountPercentage ?? 0M,
                TotalDiscountAmount = detail.DiscountAmount ?? 0M,
                OrderType = orderType,
                ItemName = detail.Name,
                ItemNameAr = detail.NameAr,
                ItemKitchenTypeId = detail.ItemKitchenTypeId,
                CategoryKitchenTypeId = detail.CategoryKitchenTypeId,
                PrintInBackupReceiptFromCategory = detail.PrintInBackupReceiptFromCategory,
                PrintInBackupReceiptFromItem = detail.PrintInBackupReceiptFromItem,
                ByWeight = detail.ByWeight,
                TotalDiscountPrice = detail.TotalDiscountPrice ?? 0M,
                TotalAfterDiscount = detail.TotalAfterDiscount,
                DiscountReason = detail.DiscountReason,
                IsVoided = detail.IsVoided,
                VoidAmount = detail.VoidAmount ?? 0,
                TotalVoidAmount = detail.TotalVoidAmount ?? 0,
                VoidBy = detail.VoidBy,
                VoidByName = detail.VoidByName,
                VoidTime = detail.VoidTime,
                VoidReason = detail.VoidReason,
                UnitPrice = detail.Price,  // ✅ إضافة حفظ سعر الوحدة
                CategoryId = detail.CategoryId,  // ✅ إضافة حفظ معرف الفئة
                CategoryName = detail.CategoryName,  // ✅ إضافة حفظ اسم الفئة
                IsHospitality = detail.IsHospitality,
                HospitalityResponsibleName = detail.HospitalityResponsibleName,
                IsStaffMeal = detail.IsStaffMeal,
                StaffMealEmployeeName = detail.StaffName,
                LineComment = !string.IsNullOrWhiteSpace(detail.LineComment)
                    ? detail.LineComment
                    : (detail.Attributes?.Any(a => a.Id >= 5000) == true
                        ? string.Join(", ", detail.Attributes.Where(a => a.Id >= 5000).Select(a => a.Name))
                        : null),
                OrderItemAttributes = detail.Attributes?
                .Where(a => a.Id < 5000)
                .Select(a => new OrderItemAttributes
                {
                    OrderItemId = 0, // Reset to 0, EF will link it to the parent correctly
                    AttributeItemId = a.Id,
                    AttributeName = a.Name ?? string.Empty,
                    ExtraPrice = a.ExtraPrice
                })
                .ToList() ?? new List<OrderItemAttributes>()
            }).ToList()
        };
    }
    private static void BackupTakeawayOrder(OrderDto OrderDto, Orders order)
    {
        order.TakeawayCustomerName = OrderDto.CustomerName;
        order.TakeawayCustomerPhone = OrderDto.CustomerPhone;
    }
    private static void BackupDineInOrder(OrderDto OrderDto, Orders order)
    {
        //order.TakerID = OrderDto.TakerID;
        order.TakerName = OrderDto.TakerName;
        order.ReservationPaid = OrderDto.ReservationPaid;
        order.ReservationRemain = OrderDto.ReservationRemain;
        order.ScheduleDateTime = OrderDto.ScheduleDateTime;
        order.CustomerCount = OrderDto.CustomerCount;
    }
    private static void BackupDeliveryOrder(OrderDto OrderDto, Orders order)
    {
        order.DeliveryCompany = OrderDto.DeliveryCompany;
        order.TitleName = OrderDto.TitleName;
        order.CustomerID = OrderDto.CustomerID;
        order.CustomerName = OrderDto.CustomerName;
        order.Phone1 = OrderDto.Phone1;
        order.Phone2 = OrderDto.Phone2;
        order.HomeNum = OrderDto.HomeNum;
        order.StreetName = OrderDto.StreetName;
        order.FloorNum = OrderDto.FloorNum;
        order.ApartmentNum = OrderDto.ApartmentNum;
        order.AddressNotice = OrderDto.AddressNotice;
        order.ZoneID = OrderDto.ZoneID;
        order.ZoneName = OrderDto.ZoneName;
        order.ZoneBonus = OrderDto.ZoneBonus;
        order.DispatchID = OrderDto.DispatchID;
        order.DriverID = OrderDto.DriverID;
        order.DriverName = OrderDto.DriverName;
        order.AssignTime = OrderDto.AssignTime;
        order.BackTime = OrderDto.BackTime;
        order.WithoutDeliveryFees = OrderDto.WithoutDeliveryFees;
        order.ClosingTime = OrderDto.ClosingTime;
        order.DeliveryBranchUrl = OrderDto.DeliveryBranchUrl;
        
        // Added missing delivery fields
        order.TakerID = OrderDto.TakerID;
        order.TakerName = OrderDto.TakerName;
        order.DeliveryFees = OrderDto.DeliveryFees;
    }

    private static OrderStates DetermineInitialState(OrderDto orderDto, OrderTypes orderType)
    {
        if (!Enum.TryParse<OrderStates>(orderDto.OrderState, out var state))
        {
            state = (orderType == OrderTypes.DineIn ? OrderStates.Pending : OrderStates.Completed);
        }

        if ((orderDto.VoidAmount ?? 0) > 0 && state != OrderStates.Dispatched && state != OrderStates.Canceled)
        {
            return OrderStates.Voided;
        }

        return state;
    }
    private async Task<List<string>> GetLogoPath(int branchId)
    {
        List<string> branchDetails = new List<string>();
        int logoWidth = 150;
        int logoHeight = 150;
        string logoPath = "";

        try 
        {
            Branch? branch = null;
            if (branchId > 0)
            {
                branch = await _branchService.GetBranchByIdAsync(branchId);
            }
            if (branch == null)
            {
                var branches = await _branchService.GetBranchesAsync();
                branch = branches?.FirstOrDefault(b => b.Id == branchId) ?? branches?.FirstOrDefault();
            }

            if (branch != null)
            {
                if (branch.LogoWidth != 0) logoWidth = branch.LogoWidth;
                if (branch.LogoHeight != 0) logoHeight = branch.LogoHeight;

                if (!string.IsNullOrEmpty(branch.ImagePath))
                {
                    var webRoot = _webHostEnvironment?.WebRootPath ?? "";
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var possiblePaths = new[]
                    {
                        Path.Combine(webRoot, "Files", "images", branch.ImagePath),
                        Path.Combine(webRoot, "images", branch.ImagePath),
                        Path.Combine(webRoot, "images", "ReceiptLogo", branch.ImagePath),
                        Path.Combine(baseDir, "wwwroot", "Files", "images", branch.ImagePath),
                        Path.Combine(baseDir, "wwwroot", "images", "ReceiptLogo", branch.ImagePath),
                        Path.Combine(baseDir, "images", "ReceiptLogo", branch.ImagePath)
                    };

                    logoPath = possiblePaths.FirstOrDefault(p => !string.IsNullOrEmpty(p) && System.IO.File.Exists(p)) ?? "";
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in GetLogoPath for branch {BranchId}", branchId);
        }

        // Fallback: search standard logo folders if no path found yet
        if (string.IsNullOrEmpty(logoPath) || !System.IO.File.Exists(logoPath))
        {
            try
            {
                var webRoot = _webHostEnvironment?.WebRootPath ?? "";
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidateDirs = new[]
                {
                    Path.Combine(baseDir, "images", "ReceiptLogo"),
                    Path.Combine(baseDir, "wwwroot", "images", "ReceiptLogo"),
                    Path.Combine(baseDir, "wwwroot", "Files", "images"),
                    Path.Combine(webRoot, "images", "ReceiptLogo"),
                    Path.Combine(webRoot, "Files", "images"),
                    Path.Combine(webRoot, "images")
                };

                foreach (var dir in candidateDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        var file = Directory.GetFiles(dir, "*.*")
                            .FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                                f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                                f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
                        if (file != null)
                        {
                            logoPath = file;
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        branchDetails.Add(logoPath);
        branchDetails.Add(logoWidth.ToString());
        branchDetails.Add(logoHeight.ToString());
        return branchDetails;
    }
    private async Task printTakeAwayReceipts(OrderDto takeawayOrder, Orders createdOrder, List<string> branchDetails, bool isFollowUp = false)
    {
        var datePart = takeawayOrder.OrderDate!.Value.Date;
        var timeNow = DateTime.Now.TimeOfDay;
        var combined = datePart.Add(timeNow);

        var receipt = new Receipt()
        {
            Id = createdOrder.OrderID,
            StoreName = takeawayOrder.BranchName!,
            CashierName = takeawayOrder.CashierName!,
            ReceiptType = OrderTypes.TakeAway.ToString(),
            DateCreated = combined,
            PaymentMethod = takeawayOrder.PaymentMethod!.ToString()!,
            FooterMessage = takeawayOrder!.FooterMessage!,
            LogoPath = branchDetails[0],
            LogoWidth = int.Parse(branchDetails[1]),
            TotalAmount = takeawayOrder.GrandTotal,
            Discount = takeawayOrder.TotalOrderDiscount,
            Tax = takeawayOrder.Tax,
            Services = takeawayOrder.Services,
            SubTotal = takeawayOrder.SubTotal,
            Items = takeawayOrder.OrderDetails!,
            IsFollowUp = isFollowUp,
            IsVoid = takeawayOrder.OrderState == OrderStates.Voided.ToString() || (takeawayOrder.VoidAmount.HasValue && takeawayOrder.VoidAmount > 0)
        };
        var receiptItems = receipt.Items;

        var outputPath = await CreateCashReceiptLayOut(receipt, receiptItems);

        await PrintCashReceipt(takeawayOrder, outputPath);
    }
    private async Task<string> CreateCashReceiptLayOut(Receipt receipt, List<TableItem>? receiptItems)
    {
        var document = await Task.Run(() => new ReceiptDocument(receipt, receiptItems!));
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-receipt.pdf");

        document.GeneratePdf(outputPath);

        return outputPath;
    }
    private async Task PrintCashReceipt(OrderDto takeawayOrder, string reportPath)
    {
        var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
        var printer = kitchens?.FirstOrDefault(k => string.Equals(k.KitchenName, "Customer", StringComparison.OrdinalIgnoreCase))
                   ?? kitchens?.ElementAtOrDefault(0);

        var deviceName = Environment.MachineName;
        var orderType = takeawayOrder.OrderType ?? OrderTypes.TakeAway.ToString();

        var receiptCount = takeawayOrder.OrderSettings?
                .Where(o => string.Equals(o.OrderType, orderType, StringComparison.OrdinalIgnoreCase))
                .Select(o => o.CustomerReceiptCount)
                .FirstOrDefault() ?? 0;

        if (receiptCount <= 0)
        {
            var branchSettings = await _orderService.GetOrderSettingsAsync(deviceName);
            receiptCount = branchSettings?
                .Where(o => string.Equals(o.OrderType, orderType, StringComparison.OrdinalIgnoreCase))
                .Select(o => o.CustomerReceiptCount)
                .FirstOrDefault() ?? 1;
        }

        if (printer == null || receiptCount <= 0)
            return;

        var targetPrinter = printer?.KitchenPrinters?.FirstOrDefault(p => string.Equals(p.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                         ?? (string.IsNullOrEmpty(takeawayOrder.MachineName) ? null : printer?.KitchenPrinters?.FirstOrDefault(p => string.Equals(p.DeviceName, takeawayOrder.MachineName, StringComparison.OrdinalIgnoreCase)))
                         ?? printer?.KitchenPrinters?.FirstOrDefault();
        if (targetPrinter == null) return;
        var printerNamesToUse = new List<string>();

        for (int i = 1; i <= receiptCount; i++)
        {
            var printerName = targetPrinter.GetType().GetProperty($"Copy{i}")?.GetValue(targetPrinter) as string;
            if (!string.IsNullOrEmpty(printerName))
            {
                printerNamesToUse.Add(printerName);
            }
        }

        foreach (var printerName in printerNamesToUse)
            await _printerServices.PrintPdfAsync(reportPath, printerName);
    }
    private async Task<string> CreateKitchenReceiptLayOut(KitchenReceipt receipt, List<TableItem>? receiptItems)
    {
        var document = await Task.Run(() => new KitchenReceiptDocument(receipt, receiptItems!));
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-receipt.pdf");

        document.GeneratePdf(outputPath);

        return outputPath;
    }
    private async Task PrintBackupReceipts( OrderDto Order, Orders createdOrder, bool isFollowUp = false, string KitchenType = "Packup", bool isCopy = false)
    {

        var filteredItems = Order.OrderDetails!
         .Where(i =>
             (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem != false) ||
             (i.PrintInBackupReceiptFromCategory == false && i.PrintInBackupReceiptFromItem == true) ||
             (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem == null)
         )
         .ToList();

        var receipt = new KitchenReceipt()
        {
            Id = createdOrder.OrderID,
            CashierName = Order.CashierName!,
            OrderType = Order.OrderType ?? OrderTypes.TakeAway.ToString(),
            DateCreated = DateTimeOffset.Now,
            Items = filteredItems,
            KitchenNote = Order.OrderNotice!,
            KitchenType = KitchenType,
            TableId = Order.TableId,
            TableName = Order.TableName,
            IsFollowUp = isFollowUp,
            IsCopy = isCopy,
            IsVoid = Order.OrderState == OrderStates.Voided.ToString() || (Order.VoidAmount.HasValue && Order.VoidAmount > 0)
        };

        if (filteredItems.Count == 0)
            return;

        var outputPath = await CreateKitchenReceiptLayOut(receipt, filteredItems);

        await PrintBackupReceipt(Order, outputPath);
    }
    private async Task PrintBackupReceipt(OrderDto takeawayOrder, string reportPath)
    {
        var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
        var printer = kitchens?.FirstOrDefault(k => string.Equals(k.KitchenName, "Backup", StringComparison.OrdinalIgnoreCase) || string.Equals(k.KitchenName, "Packup", StringComparison.OrdinalIgnoreCase))
                   ?? kitchens?.ElementAtOrDefault(1)
                   ?? kitchens?.FirstOrDefault();

        var deviceName = Environment.MachineName;
        var orderType = takeawayOrder.OrderType ?? OrderTypes.TakeAway.ToString();

        var receiptCount = takeawayOrder.OrderSettings?
                .Where(o => string.Equals(o.OrderType, orderType, StringComparison.OrdinalIgnoreCase))
                .Select(o => o.FullKitchenReceiptCount)
                .FirstOrDefault() ?? 0;

        if (receiptCount <= 0)
        {
            var branchSettings = await _orderService.GetOrderSettingsAsync(deviceName);
            receiptCount = branchSettings?
                .Where(o => string.Equals(o.OrderType, orderType, StringComparison.OrdinalIgnoreCase))
                .Select(o => o.FullKitchenReceiptCount)
                .FirstOrDefault() ?? 1;
        }

        if (printer == null || receiptCount <= 0)
            return;

        var targetPrinter = printer?.KitchenPrinters?.FirstOrDefault(p => string.Equals(p.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                         ?? (string.IsNullOrEmpty(takeawayOrder.MachineName) ? null : printer?.KitchenPrinters?.FirstOrDefault(p => string.Equals(p.DeviceName, takeawayOrder.MachineName, StringComparison.OrdinalIgnoreCase)))
                         ?? printer?.KitchenPrinters?.FirstOrDefault();
        if (targetPrinter == null) return;
        var printerNamesToUse = new List<string>();

        for (int i = 1; i <= receiptCount; i++)
        {
            var printerName = targetPrinter.GetType().GetProperty($"Copy{i}")?.GetValue(targetPrinter) as string;
            if (!string.IsNullOrEmpty(printerName))
            {
                printerNamesToUse.Add(printerName);
            }
        }

        foreach (var printerName in printerNamesToUse)
            await _printerServices.PrintPdfAsync(reportPath, printerName);
    }

    private async Task PrintKitchenReceipts( OrderDto takeawayOrder, Orders createdOrder, bool isFollowUp = false, bool isCopy = false)
    {
        var deviceName = Environment.MachineName;
        var orderType = takeawayOrder.OrderType ?? OrderTypes.TakeAway.ToString();

        var receiptCount = takeawayOrder.OrderSettings?
            .Where(o => string.Equals(o.OrderType, orderType, StringComparison.OrdinalIgnoreCase))
            .Select(o => o.SeparateReceiptCount)
            .FirstOrDefault() ?? 0;

        if (receiptCount <= 0)
        {
            var branchSettings = await _orderService.GetOrderSettingsAsync(deviceName);
            receiptCount = branchSettings?
                .Where(o => string.Equals(o.OrderType, orderType, StringComparison.OrdinalIgnoreCase))
                .Select(o => o.SeparateReceiptCount)
                .FirstOrDefault() ?? 0;
        }

        if (receiptCount <= 0)
            return;

        var items = takeawayOrder.OrderDetails;

        if (items == null || items.Count == 0)
            return;

        // --- Always override KitchenTypeId from branch local catalog ---
        // CC may send wrong/different kitchen IDs that don't match the branch's printer assignments.
        // We always resolve from the branch's own MenuSalesItems so printing uses branch config only.
        foreach (var item in items)
        {
            if (item.Id > 0)
            {
                try
                {
                    var localItem = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(item.Id);
                    if (localItem != null)
                    {
                        item.ItemKitchenTypeId = localItem.KitchenTypeId;
                        item.CategoryKitchenTypeId = localItem.Category?.KitchenTypeId;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[PrintKitchenReceipts] Could not fetch local KitchenTypeId for MenuSalesItemId={Id}", item.Id);
                }
            }
        }
        // -------------------------------------------------------------------------

        var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();

        var groupedItemsWithKitchenNameAndId = items
            .Where(item => item.ItemKitchenTypeId.HasValue || item.CategoryKitchenTypeId.HasValue)
            .GroupBy(item => item.ItemKitchenTypeId ?? item.CategoryKitchenTypeId!.Value)
            .Select(g => 
            {
                var kitchen = kitchens.FirstOrDefault(k => k.Id == g.Key);
                var printers = kitchen?.KitchenPrinters?
                    .FirstOrDefault(p => string.Equals(p.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                    ?? (string.IsNullOrEmpty(takeawayOrder.MachineName) ? null : kitchen?.KitchenPrinters?.FirstOrDefault(p => string.Equals(p.DeviceName, takeawayOrder.MachineName, StringComparison.OrdinalIgnoreCase)))
                    ?? kitchen?.KitchenPrinters?.FirstOrDefault();
                
                return new
                {
                    KitchenId = g.Key,
                    KitchenName = kitchen?.KitchenName ?? $"Kitchen_{g.Key}",
                    Items = g.ToList(),
                    Printers = printers
                };
            })
            .Where(x => x.Printers != null)
            .ToList();

        foreach (var kitchenGroup in groupedItemsWithKitchenNameAndId)
        {
            var kitchenName = kitchenGroup.KitchenName;
            var kitchenItems = kitchenGroup.Items;

            var receipt = new KitchenReceipt()
            {
                Id = createdOrder.OrderID,
                CashierName = takeawayOrder.CashierName!,
                OrderType = orderType,
                DateCreated = DateTimeOffset.Now,
                Items = kitchenItems,
                KitchenNote = takeawayOrder.OrderNotice!,
                KitchenType = kitchenName,
                TableId = takeawayOrder.TableId,
                TableName = takeawayOrder.TableName,
                IsFollowUp = isFollowUp,
                IsCopy = isCopy
            };

            var outputPath = await CreateKitchenReceiptLayOut(receipt, kitchenItems);

            var printers = kitchenGroup.Printers;
            if (printers == null) continue;
            var printerNamesToUse = new List<string>();

            for (int i = 1; i <= receiptCount; i++)
            {
                var printerName = printers.GetType().GetProperty($"Copy{i}")?.GetValue(printers) as string;
                if (!string.IsNullOrEmpty(printerName))
                {
                    printerNamesToUse.Add(printerName);
                }
            }

            foreach (var printerName in printerNamesToUse)
            {
                await _printerServices.PrintPdfAsync(outputPath, printerName);
            }
        }
    }

    [HttpPost("add-items")]
    public async Task<IActionResult> AddItemsToOrder([FromBody] OrderDto orderDto)
    {
        if (orderDto == null || !orderDto.ParentOrderId.HasValue)
            return BadRequest(new ApiResponse(400, "Parent Order ID is required for additional items."));

        // Get parent order
        var parentOrder = await _orderService.GetOrderByIdAsync(orderDto.ParentOrderId.Value);
        if (parentOrder == null) return NotFound(new ApiResponse(404, "Parent order not found."));

        if (parentOrder.OrderState == OrderStates.Dispatched || parentOrder.OrderState == OrderStates.Delivered)
            return BadRequest(new ApiResponse(400, "لا يمكن إضافة أصناف للطلب لأنه خرج للتوصيل بالفعل."));

        // Permission Check
        var settings = await _orderService.GetOrderSettingAsync(OrderTypes.Delivery, orderDto.MachineName);
        if (settings != null)
        {
            bool canAdd = _callCenterSettings.IsCentralCallCenter 
                ? (settings.CanAddItemsFromCallCenter ?? true) 
                : (settings.CanAddItemsFromBranch ?? true);

            if (!canAdd)
                return BadRequest(new ApiResponse(403, "Adding items from this device is disabled by settings."));
        }

        // Create the addition as a new linked order
        var orderItems = _mapper.Map<List<TableItem>, List<OrderItemsDetails>>(orderDto.OrderDetails!);
        foreach (var oi in orderItems)
        {
            oi.OrderType = OrderTypes.Delivery;
        }
        var order = _mapper.Map<OrderDto, Orders>(orderDto);
        order.OrderDetails = orderItems;
        order.OrderType = OrderTypes.Delivery;
        order.ParentOrderId = orderDto.ParentOrderId; // Link to parent
        order.DeliveryBranchUrl = parentOrder.DeliveryBranchUrl;
        
        var createdOrder = await _orderService.CreateOrderAsync(order);
        if (createdOrder == null) return BadRequest(new ApiResponse(500, "Failed to create addition order."));

        var resultDto = _mapper.Map<OrderDto>(createdOrder);
        // Ensure CallCenterApiUrl is carried over for sync
        resultDto.CallCenterApiUrl = parentOrder.CallCenterApiUrl ?? orderDto.CallCenterApiUrl;

        // Sync logic
        if (_callCenterSettings.IsCentralCallCenter)
        {
            // If at Call Center, send to the same branch as parent
            if (!string.IsNullOrEmpty(parentOrder.DeliveryBranchUrl))
            {
                await SendUpdateToBranch(resultDto, parentOrder.DeliveryBranchUrl, "receiveDispatchedOrder");
            }
        }
        else
        {
            // If at Branch, notify Call Center
            await SendUpdateToCallCenter(resultDto);
        }

        // Print for added items ONLY when running at the Branch.
        // When running at the Central Call Center, the branch handles printing
        // after it receives the dispatched order via ReceiveUpdateFromCallCenter.
        if (!_callCenterSettings.IsCentralCallCenter)
        {
            var orderSettings = await _orderService.GetOrderSettingsAsync(orderDto.MachineName);
            resultDto.OrderSettings = _mapper.Map<IReadOnlyList<OrderSetting>, ICollection<OrderSettingToReturnDto>>(orderSettings);


            // Also print Customer and Backup receipts for the added items
            if (orderDto.SkipPrintingOnServer != true)
            {
                if (resultDto.OrderSettings.Any(s => s.SeparateReceiptCount > 0))
                    await PrintKitchenReceipts(resultDto, createdOrder, isFollowUp: true);
                List<string> branchDetails = await GetBranchDetails(orderDto);

                if (parentOrder.OrderType == OrderTypes.Delivery)
                     await printDeliveryReceipts(resultDto, createdOrder, branchDetails, isFollowUp: true, parentDeliveryFees: parentOrder.DeliveryFees);
                else
                     await printTakeAwayReceipts(resultDto, createdOrder, branchDetails, isFollowUp: true);

                if (resultDto.OrderSettings.Any(s => s.FullKitchenReceiptCount > 0))
                     await PrintBackupReceipts(resultDto, createdOrder, isFollowUp: true, KitchenType: "Backup");
            }
        }
        else
        {
            // At CC: we still need OrderSettings populated on resultDto for the response
            var orderSettings = await _orderService.GetOrderSettingsAsync(null);
            resultDto.OrderSettings = _mapper.Map<IReadOnlyList<OrderSetting>, ICollection<OrderSettingToReturnDto>>(orderSettings);
        }

        return Ok(resultDto);
    }

    private string ResolveCallCenterApiUrl(string? currentUrl)
    {
        // 1. PRIMARY: Use the per-order CallCenterApiUrl (set by CC when dispatching to branch).
        //    This is the most authoritative source — the CC explicitly told us its own URL.
        //    CRITICAL: Branch ApiBaseUrl = branch's own URL (100.92.76.62), NOT the CC URL.
        if (!string.IsNullOrWhiteSpace(currentUrl) &&
            !currentUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) &&
            !currentUrl.Contains("127.0.0.1"))
        {
            return currentUrl.Trim().TrimEnd('/', '\\');
        }

        // 2. FALLBACK: ApiBaseUrl (only when no per-order URL is available)
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
        if (string.IsNullOrEmpty(orderDto.CallCenterApiUrl) && !orderDto.CallCenterOrderId.HasValue && !orderDto.ParentOrderId.HasValue)
        {
            Log.Debug("Order {OrderId} has no CC relationship. Skipping CC sync.", orderDto.OrderId);
            return;
        }

        // Allow if it has CallCenterOrderId OR if it is a new addition (has ParentOrderId)
        var targetUrl = ResolveCallCenterApiUrl(orderDto.CallCenterApiUrl);
        if (string.IsNullOrEmpty(targetUrl)) return;
        if (!orderDto.CallCenterOrderId.HasValue && !orderDto.ParentOrderId.HasValue) return;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var json = JsonSerializer.Serialize(orderDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync($"{targetUrl}/api/order/receiveOrderUpdate", content);
            if (response.IsSuccessStatusCode)
            {
                PendingCallCenterSyncQueue.TryRemove(orderDto.Id);
            }
            else
            {
                PendingCallCenterSyncQueue.Enqueue(orderDto);
                var error = await response.Content.ReadAsStringAsync();
                Log.Warning("[SendUpdateToCallCenter] Failed to sync order {OrderId} to CC at {Url}. Status: {StatusCode}, Error: {Error}. Queued for retry.",
                    orderDto.OrderId, targetUrl, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            PendingCallCenterSyncQueue.Enqueue(orderDto);
            Log.Error(ex, "Error syncing update to Call Center at {Url} for order {OrderId}. Queued for retry.", targetUrl, orderDto.Id);
        }
    }

    private async Task SendUpdateToBranch(OrderDto orderDto, string branchUrl, string endpoint)
    {
        if (string.IsNullOrEmpty(branchUrl)) return;

        var sanitizedUrl = branchUrl.Trim().TrimEnd('/', '\\');

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var json = JsonSerializer.Serialize(orderDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await httpClient.PostAsync($"{sanitizedUrl}/api/order/{endpoint}", content);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error syncing update to Branch {BranchUrl} for order {OrderId}", branchUrl, orderDto.OrderId);
        }
    }

    private async Task printDeliveryReceipts(OrderDto deliveryOrder, Orders createdOrder, List<string> branchDetails, bool isFollowUp = false, decimal? parentDeliveryFees = null, bool isCopy = false)
    {
        var datePart = deliveryOrder.OrderDate!.Value.Date;
        var timeNow = DateTime.Now.TimeOfDay;
        var combined = datePart.Add(timeNow);

        var receipt = new DeliveryReceipt()
        {
            Id = createdOrder.OrderID,
            RemoteOrderId = deliveryOrder.RemoteOrderId,
            ParentOrderId = deliveryOrder.ParentOrderId,
            StoreName = deliveryOrder.BranchName!,
            CashierName = deliveryOrder.CashierName!,
            ReceiptType = OrderTypes.Delivery.ToString(),
            DateCreated = combined,
            PaymentMethod = deliveryOrder.PaymentMethod!.ToString()!,
            FooterMessage = deliveryOrder!.FooterMessage!,
            LogoPath = branchDetails[0],
            LogoWidth = int.Parse(branchDetails[1]),
            TotalAmount = createdOrder.Subtotal 
                          ?? deliveryOrder.SubTotal 
                          ?? deliveryOrder.OrderDetails?.Sum(i => i.TotalAmount ?? (i.Price * i.Quantity) ?? 0),
            DeliveryFees = (createdOrder.DeliveryFees ?? deliveryOrder.DeliveryFees ?? 0) != 0 
                            ? (createdOrder.DeliveryFees ?? deliveryOrder.DeliveryFees ?? 0) 
                            : (parentDeliveryFees ?? 0),
            TotalOrder = createdOrder.GrandTotal ?? deliveryOrder.GrandTotal ?? 0,
            CustomerName = deliveryOrder.CustomerName,
            CustomerFirstPhone = deliveryOrder.Phone1,
            CustomerSecondPhone = deliveryOrder.Phone2,
            Building = deliveryOrder.HomeNum,
            FloorNumber = deliveryOrder.FloorNum,
            FlatNumber = deliveryOrder.ApartmentNum,
            ZoneName = deliveryOrder.ZoneName,
            AddressNote = deliveryOrder.AddressNotice,
            DeliveryName = deliveryOrder.DriverName,
            CustomerAddress = deliveryOrder.StreetName,
            IsFollowUp = isFollowUp,
            IsCopy = isCopy,
            IsVoid = deliveryOrder.OrderState == OrderStates.Voided.ToString() || (deliveryOrder.VoidAmount.HasValue && deliveryOrder.VoidAmount > 0)
        };

        var deliveryItems = deliveryOrder.OrderDetails!;

        var outputPath = await CreateDeliveryReceiptLayOut(receipt, deliveryItems);

        await PrintCashReceipt(deliveryOrder, outputPath);
    }

    private async Task<string> CreateDeliveryReceiptLayOut(DeliveryReceipt receipt, List<TableItem>? receiptItems)
    {
        var document = await Task.Run(() => new DeliveryReceiptDocument(receipt, receiptItems!));
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-delivery-receipt.pdf");

        document.GeneratePdf(outputPath);

        return outputPath;
    }

    private async Task NotifyAgentMachine(string method, OrderDto orderDto, string? errorMessage = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(orderDto.MachineName))
            {
                if (errorMessage != null)
                    await _deliveryHubContext.Clients.Group(orderDto.MachineName).SendAsync(method, orderDto, errorMessage);
                else
                    await _deliveryHubContext.Clients.Group(orderDto.MachineName).SendAsync(method, orderDto);
            }
            else
            {
                if (errorMessage != null)
                    await _deliveryHubContext.Clients.All.SendAsync(method, orderDto, errorMessage);
                else
                    await _deliveryHubContext.Clients.All.SendAsync(method, orderDto);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending Hub notification {Method} for order {OrderId}", method, orderDto.OrderId);
        }
    }
}

