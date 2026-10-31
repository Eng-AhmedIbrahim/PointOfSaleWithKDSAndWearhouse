namespace POS.API.Controllers.VoidControllers;

public class VoidController : BaseApiController
{
    private readonly IVoidService _voidService;
    private readonly IOrderService _orderService;
    private readonly IMapper _mapper;
    private readonly IKitchenServices _kitchenServices;
    private readonly IPrinterServices _printerServices;
    private readonly CallCenterSettings _callCenterSettings;
    private readonly IHubContext<DeliveryHub> _deliveryHubContext;
    private readonly string _reportsFolder;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;

    public VoidController(IVoidService voidService,
        IOrderService orderService,
        IMapper mapper,
        IKitchenServices kitchenServices,
        IPrinterServices printerServices,
        CallCenterSettings callCenterSettings,
        IHubContext<DeliveryHub> deliveryHubContext,
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration)
    {
        _voidService = voidService;
        _orderService = orderService;
        _mapper = mapper;
        _kitchenServices = kitchenServices;
        _printerServices = printerServices;
        _callCenterSettings = callCenterSettings;
        _deliveryHubContext = deliveryHubContext;
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
        _reportsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports");
    }

    [HttpDelete("voidOrder/{id}")]
    public async Task<ActionResult<bool>> VoidOrder(int id,
        [FromQuery] string reason, 
        [FromQuery] string voidBy, 
        [FromQuery] string voidByName,
        [FromQuery] bool returnToStock = false)
    {
        var result = await _voidService.VoidOrderAsync(id, reason, voidBy, voidByName, returnToStock);
        
        if (result)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order != null)
            {
                var orderDto = _mapper.Map<OrderDto>(order);
                
                // 1. Kitchen Printing - ONLY if NOT in Central Call Center
                // If in CC, the branch will handle the printing when it receives the sync
                // Handled by Desktop UI / DesktopPrintOrderService to avoid double printing
                // if (!_callCenterSettings.IsCentralCallCenter)
                // {
                //     await PrintVoidToKitchen(orderDto, order.OrderDetails?.ToList());
                // }
                // 2. Broadcast void on local hub so ALL connected clients on this machine
                //    (other CC/branch screens) update their distribution list immediately
                await _deliveryHubContext.Clients.All.SendAsync("ReceiveOrderUpdated", orderDto);

                // 3. Sync to Call Center / Branch
                await SyncVoidUpdate(orderDto);
            }
        }
        
        return Ok(result);
    }

    [HttpPost("voidItems/{id}")]
    public async Task<ActionResult<bool>> VoidItems(int id, 
        [FromBody] List<OrderItemVoidDto> itemsToVoid, 
        [FromQuery] string reason, 
        [FromQuery] string voidBy, 
        [FromQuery] string voidByName,
        [FromQuery] bool returnToStock = false)
    {
        var result = await _voidService.VoidItemsAsync(id, itemsToVoid, reason, voidBy, voidByName, returnToStock);
        
        if (result)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order != null)
            {
                var orderDto = _mapper.Map<OrderDto>(order);
                
                // Identify items that were just voided to print them
                var voidedDetailIds = itemsToVoid.Select(i => i.OrderItemDetailId).ToList();
                var itemsForKitchen = order.OrderDetails?
                    .Where(d => voidedDetailIds.Contains(d.Id))
                    .ToList();

                // Handled by Desktop UI / DesktopPrintOrderService to avoid double printing
                // if (!_callCenterSettings.IsCentralCallCenter && itemsForKitchen != null && itemsForKitchen.Any())
                // {
                //     await PrintVoidToKitchen(orderDto, itemsForKitchen);
                // }

                // 2. Broadcast void on local hub so ALL connected clients on this machine
                //    (other CC/branch screens) update their distribution list immediately
                await _deliveryHubContext.Clients.All.SendAsync("ReceiveOrderUpdated", orderDto);

                // 3. Sync to Call Center / Branch
                await SyncVoidUpdate(orderDto);
            }
        }
        
        return Ok(result);
    }

    [HttpGet("report")]
    public async Task<ActionResult<List<VoidReportDto>>> GetVoidReport([FromQuery] DateTime posDate)
    {
        var report = await _voidService.GetVoidReportAsync(posDate);
        return Ok(report);
    }

    private async Task SyncVoidUpdate(OrderDto orderDto)
    {
        if (_callCenterSettings.IsCentralCallCenter)
        {
            // If we are in CC, notify the branch
            if (!string.IsNullOrEmpty(orderDto.DeliveryBranchUrl))
            {
                await SendUpdateToBranch(orderDto, orderDto.DeliveryBranchUrl, "receiveUpdateFromCallCenter");
            }
        }
        else
        {
            // If we are in Branch, notify CC
            await SendUpdateToCallCenter(orderDto);
        }
    }

    private string ResolveCallCenterApiUrl(string? currentUrl)
    {
        // 1. PRIMARY: Use the per-order CallCenterApiUrl (set by CC when dispatching to branch).
        //    This is the most authoritative source — the CC explicitly told us its own URL.
        //    NOTE: Branch ApiBaseUrl points to the branch itself — NOT the CC.
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
                Log.Warning("[VoidController] Failed to sync void update to CC at {Url}. Status: {StatusCode}, Error: {Error}. Queued for retry.",
                    targetUrl, response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            PendingCallCenterSyncQueue.Enqueue(orderDto);
            Log.Error(ex, "Error syncing void update to Call Center at {Url} for order {OrderId}. Queued for retry.", targetUrl, orderDto.Id);
        }
    }

    private async Task SendUpdateToBranch(OrderDto orderDto, string branchUrl, string endpoint)
    {
        if (string.IsNullOrEmpty(branchUrl)) return;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var json = JsonSerializer.Serialize(orderDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await httpClient.PostAsync($"{branchUrl}/api/order/{endpoint}", content);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error syncing void update to Branch {BranchUrl} for order {OrderId}", branchUrl, orderDto.OrderId);
        }
    }

    // Helper method that can be called internally or potentially publicly if moved to a service
    private async Task PrintVoidToKitchen(OrderDto orderDto, List<OrderItemsDetails>? itemsToVoid)
    {
        try 
        {
            if (itemsToVoid == null || !itemsToVoid.Any()) return;

            var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
            var kitchenItems = _mapper.Map<List<OrderItemsDetails>, List<TableItem>>(itemsToVoid);

            // Fetch order settings to get FullKitchenReceiptCount for the correct order type
            var orderSettings = await _orderService.GetOrderSettingsAsync(null);
            var settingsOrderType = (orderDto.OrderType == "Staff" || orderDto.OrderType == "Hospitality") ? "TakeAway" : (orderDto.OrderType ?? "TakeAway");
            var fullKitchenCount = orderSettings?.FirstOrDefault(s => string.Equals(s.OrderType, settingsOrderType, StringComparison.OrdinalIgnoreCase))?.FullKitchenReceiptCount ?? 1;

            // Group by kitchen
            var groupedItems = kitchenItems
                .Where(item => item.ItemKitchenTypeId.HasValue || item.CategoryKitchenTypeId.HasValue)
                .GroupBy(item => item.ItemKitchenTypeId ?? item.CategoryKitchenTypeId!.Value)
                .Select(g => 
                {
                    var kitchen = kitchens.FirstOrDefault(k => k.Id == g.Key);
                    var printers = kitchen?.KitchenPrinters?
                        .FirstOrDefault(p => p.DeviceName == (orderDto.MachineName ?? Environment.MachineName));
                    
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
                    KitchenNote = $"*** VOID/ملغي ***\nReason: {orderDto.VoidReason}",
                    KitchenType = group.KitchenName,
                    TableId = orderDto.TableId,
                    TableName = orderDto.TableName,
                    IsVoid = true
                };

                var document = new KitchenReceiptDocument(receipt, group.Items);
                var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss_fff");
                var outputPath = Path.Combine(_reportsFolder, $"VOID_{timestamp}_{receipt.Id}.pdf");
                
                Directory.CreateDirectory(_reportsFolder);
                document.GeneratePdf(outputPath);

                var printers = group.Printers;
                for (int i = 1; i <= fullKitchenCount; i++)
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
            Log.Error(ex, "Error printing void to kitchen for order {OrderId}", orderDto.OrderId);
        }
    }
}
