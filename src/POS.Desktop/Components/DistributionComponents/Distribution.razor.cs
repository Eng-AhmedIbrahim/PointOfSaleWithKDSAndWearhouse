using BlazorBase.Components.Shared;
using BlazorBase.ERPFrontServices.BranchServices;
using BlazorBase.ERPFrontServices.SettingsServices;
using POS.Contract.Dtos.SettingsDtos;

namespace POS.Desktop.Components.DistributionComponents;

public partial class Distribution : IDisposable, IAsyncDisposable
{
    [Inject] public IDistributionErpService _distributionService { get; set; } = default!;
    [Inject] public IBranchService _branchService { get; set; } = default!;
    [Inject] public CallCenterHubSettings _hubSettings { get; set; } = default!;
    [Inject] public ISystemSettingsServices _systemSettingsServices { get; set; } = default!;
    [Inject] public IPosFeatureSettingsService _featureSettingsService { get; set; } = default!;
    private DispatcherSettingsDto _dynamicDispatcherSettings = new();
    [Inject] public CommonProperties _commonProperties { get; set; } = default!;
    [Inject] public NavigationManager navigationManager { get; set; } = default!;
    [Inject] public CartService _cartService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IDialogService _dialogService { get; set; } = default!;
    [Inject] public IPrintOrderService _printOrderService { get; set; } = default!;
    [Inject] public IAppDateService _appDateService { get; set; } = default!;
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    // Permission flags
    private bool _canAssignDriver;
    private bool _canViewOrder;
    private bool _canVoidOrder;
    private bool _canPrintOrder;
    private bool _canUnDispatch;
    private bool _canCollect;
    private bool _canViewVoidHistory;
    private bool _canViewDriverSettlement;
    private bool _canViewDrivers;
    private bool _canPosSettingsFeature;
    private bool _isCallCenter;



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
            catch { /* fall through */ }

            try { Console.Beep(1000, 1000); }
            catch { System.Media.SystemSounds.Asterisk.Play(); }
        });
    }

    private async Task ShowDriverSettlement()

    {
        var appDate = await _appDateService.GetAppDate();
        var settlements = await _distributionService.GetDriverSettlement(appDate.PosDate);

        var parameters = new DialogParameters<DriversSettlementDialog>();
        parameters.Add("Settlements", settlements);
        parameters.Add("PosDate", appDate.PosDate);

        await _dialogService.ShowAsync<DriversSettlementDialog>(Localizer["Distribution_DriverSettlement"], parameters);
    }

    private async Task PrintOrder(OrderDto order)
    {
        await _printOrderService.PrintDispatchOrderAsync(order);
    }

    private List<HubConnection> _externalHubConnections = new();
    private List<OrderDto> Orders = new();
    private Timer? _timer;

    // Branch filter (null = all branches)
    private int? _selectedBranchId = null;
    private string _searchQuery = "";

    private List<(int Id, string Name)> _allBranchesList = new();

    // Available branches derived from loaded branches and orders
    private IEnumerable<(int Id, string Name)> AvailableBranches
    {
        get
        {
            var dict = new Dictionary<int, string>();
            foreach (var b in _allBranchesList)
            {
                dict[b.Id] = b.Name;
            }
            foreach (var o in Orders.Where(o => o.BranchId > 0 && !string.IsNullOrEmpty(o.BranchName)))
            {
                dict[o.BranchId] = o.BranchName!;
            }
            return dict.Select(kv => (Id: kv.Key, Name: kv.Value)).OrderBy(b => b.Name).ToList();
        }
    }

    private bool IsOrderAssigned(OrderDto o)
    {
        return !string.IsNullOrEmpty(o.DriverName) 
            || !string.IsNullOrEmpty(o.DriverID) 
            || o.OrderState == "Dispatched" 
            || o.OrderState == "Delivering";
    }

    private IEnumerable<OrderDto> FilteredOrders
    {
        get
        {
            IEnumerable<OrderDto> q;

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                var term = _searchQuery.Trim();
                // Search across ALL delivery orders (both assigned to driver or unassigned)
                q = Orders.Where(o =>
                    (!string.IsNullOrEmpty(o.Phone1) && o.Phone1.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.Phone2) && o.Phone2.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.CustomerPhone) && o.CustomerPhone.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.TakeawayCustomerPhone) && o.TakeawayCustomerPhone.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.CustomerName) && o.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(o.TakeAwayCustomerName) && o.TakeAwayCustomerName.Contains(term, StringComparison.OrdinalIgnoreCase))
                );
            }
            else
            {
                q = showAssigned
                    ? Orders.Where(IsOrderAssigned)
                    : Orders.Where(o => !IsOrderAssigned(o));
            }

            if (_selectedBranchId.HasValue)
                q = q.Where(o => o.BranchId == _selectedBranchId.Value);

            return q;
        }
    }

    private HashSet<int> SelectedOrderIds = new();
    private bool SelectAllOrders = false;
    private bool showAssigned = false;

    private IEnumerable<OrderDto> SelectedOrders => FilteredOrders.Where(o => SelectedOrderIds.Contains(o.Id));

    private string? _selectedDriverId;
    private List<OrderDto> BusyDrivers => Orders.Where(o => !string.IsNullOrEmpty(o.DriverID))
                                               .Select(o => new OrderDto { DriverID = o.DriverID, DriverName = o.DriverName })
                                               .DistinctBy(d => d.DriverID)
                                               .ToList();

    // Dialog state variables
    private bool showDriversDialog = false;
    private string driversDialogTitle = "";

    private bool showAssignmentDialog = false;
    private OrderDto? selectedOrder;
    private string? selectedDriver;


    private Task NavigateBack()
    {
        _commonProperties.CurrentPosMode = "TakeAway";
        navigationManager.NavigateTo("/pos");
        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Load permissions
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            // Run all permission checks in parallel instead of sequentially
            var permTasks = new[]
            {
                AuthorizationService.AuthorizeAsync(user, "CanAccessDistributionAssignBtn"),
                AuthorizationService.AuthorizeAsync(user, "CanAccessDistributionViewBtn"),
                AuthorizationService.AuthorizeAsync(user, "CanAccessDistributionVoidBtn"),
                AuthorizationService.AuthorizeAsync(user, "CanAccessDistributionPrintBtn"),
                AuthorizationService.AuthorizeAsync(user, "CanAccessDistributionUnDispatchBtn"),
                AuthorizationService.AuthorizeAsync(user, "CanAccessDistributionCollectBtn"),
                AuthorizationService.AuthorizeAsync(user, "CanAccessDistributionVoidHistoryBtn"),
                AuthorizationService.AuthorizeAsync(user, "CanAccessDistributionDriverSettlementBtn"),
                AuthorizationService.AuthorizeAsync(user, "CanAccessDistributionViewDriversBtn"),
                AuthorizationService.AuthorizeAsync(user, "CanAccessPosSettingsFeature"),
            };
            var permResults = await Task.WhenAll(permTasks);

            _canAssignDriver          = permResults[0].Succeeded;
            _canViewOrder             = permResults[1].Succeeded;
            _canVoidOrder             = permResults[2].Succeeded;
            _canPrintOrder            = permResults[3].Succeeded;
            _canUnDispatch            = permResults[4].Succeeded;
            _canCollect               = permResults[5].Succeeded;
            _canViewVoidHistory       = permResults[6].Succeeded;
            _canViewDriverSettlement  = permResults[7].Succeeded;
            _canViewDrivers           = permResults[8].Succeeded;
            _canPosSettingsFeature    = permResults[9].Succeeded;
        }

        // --- Call Center Override ---
        bool isCallCenter = await _featureSettingsService.IsFeatureEnabledAsync("IsCallCenter", Environment.MachineName);
        _isCallCenter = isCallCenter;
        if (isCallCenter)
        {
            _canAssignDriver = false;
            _canUnDispatch = false;
            _canCollect = false;
            _canViewDriverSettlement = false;
            _canViewDrivers = false;
        }

        // Connect to hubs in background (non-blocking) so the UI can render immediately
        _ = Task.Run(async () => await ConnectToExternalHubs());

        // Fetch orders, drivers, settings, and branches in parallel
        var ordersTask   = _distributionService.GetUnCompletedDeliveryOrders();
        var driversTask  = _commonProperties.Drivers.Any()
                            ? Task.FromResult<ICollection<POS.Contract.Dtos.DineInDtos.UserToReturnDto>>(new List<POS.Contract.Dtos.DineInDtos.UserToReturnDto>())
                            : _distributionService.GetDeliveryUsers();
        var settingsTask = _systemSettingsServices.GetDispatcherSettingsAsync();
        var branchesTask = _branchService.GetBranches();

        await Task.WhenAll(ordersTask, driversTask, settingsTask, branchesTask);

        try
        {
            var branchList = branchesTask.Result;
            if (branchList != null)
            {
                _allBranchesList = branchList
                    .Where(b => !string.IsNullOrWhiteSpace(b.Name))
                    .Select(b => (Id: b.Id, Name: b.Name!.Trim()))
                    .DistinctBy(b => b.Id)
                    .ToList();
            }
        }
        catch { /* continue */ }

        var orders = ordersTask.Result;
        if (orders != null)
        {
            foreach (var order in orders)
                AddNewDeliveryOrder(order);
        }

        if (!_commonProperties.Drivers.Any())
        {
            foreach (var driver in driversTask.Result)
                _commonProperties.Drivers.Add(driver, "Available");
        }

        UpdateDriverStatus();

        _dynamicDispatcherSettings = settingsTask.Result;

        _timer = new Timer(_ =>
        {
            InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(_dynamicDispatcherSettings.RefreshTimeForDeliveryOrderColorsPerSecond));
    }

    private async Task ConnectToExternalHubs()
    {
        // Clean up any existing connections before establishing new ones to prevent duplicate event listeners
        if (_externalHubConnections.Any())
        {
            await DisposeAsync();
        }

        // الـ Hub المسجّل في POS.API هو /callcenterhub
        // نستخدم الـ URL من appsettings مباشرة (CallCenterHubs:Urls)
        var urls = (_hubSettings.Urls ?? new())
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Distinct()
                    .ToList();

        foreach (var hubUrl in urls)
        {
            if (string.IsNullOrWhiteSpace(hubUrl))
                continue;

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            connection.On<OrderDto>("ReceiveNewDeliveryOrder", async orderDto =>
            {
                Serilog.Log.Information($"[Distribution] New external order received from {hubUrl}: OrderId={orderDto.OrderId}");
                
                // Play bell and Print if this machine is a Dispatcher
                try
                {
                    var settings = await _systemSettingsServices.GetDispatcherSettingsAsync();
                    if (!_isCallCenter && settings != null && settings.IsDispatcher)
                    {
                        Serilog.Log.Information($"[Distribution] IsDispatcher=true → Playing bell and starting PrintReceivedOrderAsync for Order #{orderDto.OrderId}");
                        PlayBellForOneSecond();
                        await _printOrderService.PrintReceivedOrderAsync(orderDto);
                        Serilog.Log.Information($"[Distribution] PrintReceivedOrderAsync triggered successfully for Order #{orderDto.OrderId}");
                    }
                    else
                    {
                        Serilog.Log.Information($"[Distribution] IsDispatcher=false → Skipping bell and print for Order #{orderDto.OrderId}");
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, $"[Distribution] Error playing bell or printing Order #{orderDto.OrderId}");
                }

                await InvokeAsync(() =>
                {
                    AddNewDeliveryOrder(orderDto);
                    UpdateDriverStatus();
                    StateHasChanged();
                });
            });

            connection.On<OrderDto>("OrderDispatchedCentralNotification", orderDto =>
            {
                Console.WriteLine($"Central Notification Order Dispatched: {orderDto.OrderId}");
                InvokeAsync(() =>
                {
                    var existing = Orders.FirstOrDefault(o => o.Id == orderDto.Id || (orderDto.CallCenterOrderId.HasValue && o.CallCenterOrderId == orderDto.CallCenterOrderId.Value));
                    if (existing != null)
                    {
                        existing.OrderState = !_isCallCenter ? "Assigned" : (orderDto.OrderState ?? "SentToBranch");
                        existing.CallCenterOrderId = orderDto.CallCenterOrderId;
                    }
                    else
                    {


                        AddNewDeliveryOrder(orderDto);
                    }
                    UpdateDriverStatus();
                    StateHasChanged();
                });
            });

            connection.On<OrderDto, string>("OrderDispatchFailedCentralNotification", (orderDto, error) =>
            {
                Console.WriteLine($"Central Notification Dispatch Failed: {orderDto.OrderId}");
                InvokeAsync(() =>
                {
                    var existing = Orders.FirstOrDefault(o => o.Id == orderDto.Id || (orderDto.CallCenterOrderId.HasValue && o.CallCenterOrderId == orderDto.CallCenterOrderId.Value));
                    if (existing != null)
                    {
                        existing.OrderState = "FailedToDeliverToBranch";
                    }
                    else
                    {
                        orderDto.OrderState = "FailedToDeliverToBranch";


                        AddNewDeliveryOrder(orderDto);
                    }
                    UpdateDriverStatus();
                    StateHasChanged();
                });
            });

            connection.On<OrderDto>("ReceiveOrderDispatched", orderDto =>
            {
                Console.WriteLine($"Order dispatched: {orderDto.OrderId} to driver {orderDto.DriverName}");
                InvokeAsync(() =>
                {
                    UpdateOrderStatus(orderDto);
                    UpdateDriverStatus();
                });
            });

            connection.On<int>("ReceiveOrderUnDispatched", id =>
            {
                Console.WriteLine($"Order un-dispatched: {id}");
                InvokeAsync(() =>
                {
                    var order = Orders.FirstOrDefault(o => o.Id == id || (o.CallCenterOrderId.HasValue && o.CallCenterOrderId == id));
                    if (order != null)
                    {
                        order.DriverID = null;
                        order.DriverName = null;
                        order.AssignTime = null;
                        order.DispatchID = null;
                        order.OrderState = _isCallCenter ? "SentToBranch" : "Assigned";
                        UpdateDriverStatus();
                        StateHasChanged();
                    }
                });
            });

            connection.On<OrderDto>("ReceiveOrderCollected", orderDto =>
            {
                Console.WriteLine($"Order collected: {orderDto.Id}");
                InvokeAsync(() =>
                {
                    RemoveOrder(orderDto.Id);
                    UpdateDriverStatus();
                });
            });

            connection.On<OrderDto>("ReceiveOrderUpdated", async orderDto =>
            {
                Console.WriteLine($"Order updated: {orderDto.OrderId}, State: {orderDto.OrderState}");
                
                if (orderDto.OrderState == "Voided")
                {
                    try
                    {
                        var settings = await _systemSettingsServices.GetDispatcherSettingsAsync();
                        if (!_isCallCenter && settings != null && settings.IsDispatcher)
                        {
                            PlayBellForOneSecond();
                            await _printOrderService.PrintVoidReceiptAsync(orderDto, orderDto.OrderDetails ?? new());
                        }
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, $"[Distribution] Error playing bell or printing void for Order #{orderDto.OrderId}");
                    }
                }

                await InvokeAsync(() =>
                {
                    if (orderDto.OrderState == "Completed" || orderDto.OrderState == "Voided")
                    {
                        var toRemove = Orders.FirstOrDefault(o => o.Id == orderDto.Id 
                            || (orderDto.CallCenterOrderId.HasValue && o.CallCenterOrderId == orderDto.CallCenterOrderId.Value)
                            || o.CallCenterOrderId == orderDto.Id);

                        if (toRemove != null)
                        {
                            RemoveOrder(toRemove.Id);
                        }
                    }
                    else
                    {
                        var existingOrder = Orders.FirstOrDefault(o => o.Id == orderDto.Id 
                            || (orderDto.CallCenterOrderId.HasValue && o.CallCenterOrderId == orderDto.CallCenterOrderId.Value)
                            || o.CallCenterOrderId == orderDto.Id);

                        if (existingOrder != null)
                        {
                            existingOrder.DriverName = orderDto.DriverName;
                            existingOrder.DriverID = orderDto.DriverID;
                            if (string.IsNullOrEmpty(existingOrder.DriverName) && !string.IsNullOrEmpty(existingOrder.DriverID))
                            {
                                var driver = _commonProperties.Drivers.Keys.FirstOrDefault(d => d.Id == existingOrder.DriverID);
                                if (driver != null)
                                {
                                    existingOrder.DriverName = !string.IsNullOrEmpty(driver.ArabicName) 
                                        ? driver.ArabicName 
                                        : (!string.IsNullOrEmpty(driver.DisplayName) ? driver.DisplayName : driver.UserName);
                                }
                            }
                            if (!_isCallCenter && orderDto.OrderState == "SentToBranch")
                            {
                                existingOrder.OrderState = "Assigned";
                            }
                            else
                            {
                                existingOrder.OrderState = orderDto.OrderState;
                            }
                            existingOrder.AssignTime = orderDto.AssignTime;
                            existingOrder.DispatchID = orderDto.DispatchID;
                            
                            // Update order details if modified; ring bell so Dispatcher is alerted
                            if (orderDto.OrderDetails != null && orderDto.OrderDetails.Any())
                            {
                                existingOrder.OrderDetails = orderDto.OrderDetails;
                                existingOrder.GrandTotal = orderDto.GrandTotal;
                                existingOrder.SubTotal = orderDto.SubTotal;
                                // New items were added to this order from CC – alert the dispatcher
                                PlayBellForOneSecond();
                            }
                        }
                        else
                        {
    

                        AddNewDeliveryOrder(orderDto);
                        }
                        UpdateDriverStatus();
                        StateHasChanged();
                    }
                });
            });

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await connection.StartAsync(cts.Token);
                Console.WriteLine($"Connected to hub: {hubUrl}");
                _externalHubConnections.Add(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to hub {hubUrl}: {ex.Message}");
            }
        }
    }

    private void AddNewDeliveryOrder(OrderDto newOrder)
    {
        var existing = Orders.FirstOrDefault(o => o.Id == newOrder.Id 
            || (newOrder.CallCenterOrderId.HasValue && o.CallCenterOrderId == newOrder.CallCenterOrderId.Value)
            || o.CallCenterOrderId == newOrder.Id
            || (newOrder.CallCenterOrderId.HasValue && o.Id == newOrder.CallCenterOrderId.Value));

        if (string.IsNullOrEmpty(newOrder.DriverName) && !string.IsNullOrEmpty(newOrder.DriverID))
        {
            var driver = _commonProperties.Drivers.Keys.FirstOrDefault(d => d.Id == newOrder.DriverID);
            if (driver != null)
            {
                newOrder.DriverName = !string.IsNullOrEmpty(driver.ArabicName) 
                    ? driver.ArabicName 
                    : (!string.IsNullOrEmpty(driver.DisplayName) ? driver.DisplayName : driver.UserName);
            }
        }

        if (!_isCallCenter && newOrder.OrderState == "SentToBranch")
        {
            newOrder.OrderState = "Assigned";
        }
        if (existing == null)
        {
            if (string.IsNullOrEmpty(newOrder.DriverName))
                newOrder.DriverName = null;

            Orders.Insert(0, newOrder);
            UpdateDriverStatus();
            InvokeAsync(StateHasChanged);
        }
        else
        {
            existing.OrderState = newOrder.OrderState;
            existing.DriverName = newOrder.DriverName;
            existing.DriverID = newOrder.DriverID;
            existing.AssignTime = newOrder.AssignTime;
            UpdateDriverStatus();
            InvokeAsync(StateHasChanged);
        }
    }

    private string GetStateText(string? state) => state switch
    {
        "SentToBranch" => "وصل للفرع",
        "FailedToDeliverToBranch" => "فشل الإرسال للفرع",
        "Assigned" => "تحت التجهيز",
        "Dispatched" => "خرج للتوصيل",
        "Completed" => "تم التسليم",
        "Voided" => "ملغي",
        "Pending" => "تحت التجهيز",
        _ => "تحت التجهيز"
    };

    private Color GetStateColor(string? state) => state switch
    {
        "SentToBranch" => Color.Warning,
        "FailedToDeliverToBranch" => Color.Error,
        "Dispatched" => Color.Info,
        "Assigned" => Color.Primary,
        "Completed" => Color.Success,
        "Voided" => Color.Warning,
        _ => Color.Default
    };

    private void UpdateOrderStatus(OrderDto updatedOrder)
    {
        // Match by local Id first; fall back to CallCenterOrderId cross-matching so the CC
        // correctly identifies branch-originated ReceiveOrderDispatched events where the
        // incoming Id is the branch PK (stored as CallCenterOrderId on the CC copy).
        var existingOrder = Orders.FirstOrDefault(o =>
            o.Id == updatedOrder.Id
            || (updatedOrder.CallCenterOrderId.HasValue && o.CallCenterOrderId == updatedOrder.CallCenterOrderId.Value)
            || o.CallCenterOrderId == updatedOrder.Id
            || (updatedOrder.CallCenterOrderId.HasValue && o.Id == updatedOrder.CallCenterOrderId.Value));

        if (existingOrder != null)
        {
            existingOrder.DriverName = updatedOrder.DriverName;
            existingOrder.DriverID = updatedOrder.DriverID;
            if (string.IsNullOrEmpty(existingOrder.DriverName) && !string.IsNullOrEmpty(existingOrder.DriverID))
            {
                var driver = _commonProperties.Drivers.Keys.FirstOrDefault(d => d.Id == existingOrder.DriverID);
                if (driver != null)
                {
                    existingOrder.DriverName = !string.IsNullOrEmpty(driver.ArabicName) 
                        ? driver.ArabicName 
                        : (!string.IsNullOrEmpty(driver.DisplayName) ? driver.DisplayName : driver.UserName);
                }
            }
            if (!_isCallCenter && updatedOrder.OrderState == "SentToBranch")
            {
                existingOrder.OrderState = "Assigned";
            }
            else
            {
                existingOrder.OrderState = updatedOrder.OrderState;
            }
            existingOrder.AssignTime = updatedOrder.AssignTime;
            existingOrder.DispatchID = updatedOrder.DispatchID;
            UpdateDriverStatus();
            InvokeAsync(StateHasChanged);
        }
    }

    private void RemoveOrder(int id)
    {
        Orders.RemoveAll(o => o.Id == id);
        UpdateDriverStatus();
        InvokeAsync(StateHasChanged);
    }

    private void UpdateDriverStatus()
    {
        try
        {
            var busyDriverIds = Orders
                .Where(o => !string.IsNullOrEmpty(o.DriverID))
                .Select(o => o.DriverID)
                .ToHashSet();

            foreach (var driverKey in _commonProperties.Drivers.Keys.ToList())
            {
                if (busyDriverIds.Contains(driverKey.Id))
                {
                    _commonProperties.Drivers[driverKey] = "Unavailable";
                }
                else
                {
                    _commonProperties.Drivers[driverKey] = "Available";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating driver status: {ex.Message}");
        }
    }

    private void OnOrderCollectedHandler(OrderDto collectedOrder)
    {
        Orders.RemoveAll(o => o.Id == collectedOrder.Id);
        StateHasChanged();
    }

    private void ToggleSelectAll(bool value)
    {
        SelectAllOrders = value;
        var currentOrders = FilteredOrders.ToList();
        if (value)
            SelectedOrderIds = new HashSet<int>(currentOrders.Select(o => o.Id));
        else
            SelectedOrderIds.Clear();
        StateHasChanged();
    }

    private void ToggleOrderSelection(OrderDto order, bool value)
    {
        if (value)
            SelectedOrderIds.Add(order.Id);
        else
            SelectedOrderIds.Remove(order.Id);

        var currentOrders = FilteredOrders.ToList();
        SelectAllOrders = currentOrders.Any() && SelectedOrderIds.Count == currentOrders.Count;
        StateHasChanged();
    }

    private void PrepareCollection(OrderDto order)
    {
        order.BackTime = DateTime.Now;
        order.CollectorID = _commonProperties.CurrentUserId;
        order.CollectorName = _commonProperties.CurrentUser;
    }

    private async Task CollectSelected()
    {
        var selected = SelectedOrders.ToList();
        if (!selected.Any())
        {
            Snackbar.Add(Localizer["Distribution_NoOrdersSelected"], Severity.Warning);
            return;
        }

        foreach (var order in selected)
        {
            PrepareCollection(order);
            await _distributionService.CollectDeliveryOrder(order);
        }

        Snackbar.Add(Localizer["Distribution_CollectedSelectedSuccess"], Severity.Success);
        await RefreshData();
    }

    private async Task CollectByDriver(string driverId)
    {
        if (string.IsNullOrEmpty(driverId)) return;

        var ordersToCollect = Orders.Where(o => o.DriverID == driverId && o.OrderState == "Dispatched" || o.OrderState == "Delivering").ToList();
        if (!ordersToCollect.Any())
        {
            ordersToCollect = Orders.Where(o => o.DriverID == driverId).ToList();
        }

        foreach (var order in ordersToCollect)
        {
            PrepareCollection(order);
            await _distributionService.CollectDeliveryOrder(order);
        }

        Snackbar.Add(Localizer["Distribution_CollectedDriverSuccess"], Severity.Success);
        await RefreshData();
    }

    private void ToggleAssigned(bool assigned)
    {
        _searchQuery = string.Empty;
        showAssigned = assigned;
        SelectedOrderIds.Clear();
        SelectAllOrders = false;
        StateHasChanged();
    }

    private async Task RefreshData()
    {
        Snackbar.Add(Localizer["Distribution_Updating"], Severity.Info);
        var orders = await _distributionService.GetUnCompletedDeliveryOrders();
        Orders.Clear();
        foreach (var order in orders)
            AddNewDeliveryOrder(order);

        SelectedOrderIds.Clear();
        SelectAllOrders = false;
        showDriversDialog = false;
        showAssignmentDialog = false;
        Snackbar.Add(Localizer["Distribution_Updated"], Severity.Success);
        StateHasChanged();
    }

    private async Task UnDispatchOrder(OrderDto order)
    {
        var result = await _distributionService.UnDispatchOrder(order.Id);
        if (result)
        {
            order.DriverID = null;
            order.DriverName = null;
            order.AssignTime = null;
            order.DispatchID = null;
            order.OrderState = _isCallCenter ? "SentToBranch" : "Assigned";
            UpdateDriverStatus();
            Snackbar.Add(Localizer["Distribution_UnDispatchedSuccess"], Severity.Success);
            StateHasChanged();
        }
    }

    private async Task CollectAll()
    {
        var dispatchedOrders = FilteredOrders.Where(o => !string.IsNullOrEmpty(o.DriverID)).ToList();
        if (!dispatchedOrders.Any())
        {
            Snackbar.Add(Localizer["Distribution_NoOrdersSelected"], Severity.Warning);
            return;
        }

        foreach (var order in dispatchedOrders)
        {
            PrepareCollection(order);
            await _distributionService.CollectDeliveryOrder(order);
        }

        Snackbar.Add(Localizer["Distribution_CollectedAllSuccess"], Severity.Success);
        await RefreshData();
    }

    private void ExitApp()
    {
        navigationManager.NavigateTo("/");
    }

    private async Task OpenAssignmentDialog(OrderDto order)
    {
        selectedOrder = order;
        selectedDriver = null;
        showAssignmentDialog = true;

        var parameters = new DialogParameters<ChoiceDriverDialog>();
        parameters.Add(x => x.OrderClientName, order.CustomerName);
        parameters.Add(x => x.CurrentDeliveryOrder, order);
        parameters.Add(x => x.OrderAddress, $"{order.AddressNotice}-{order.StreetName}");
        parameters.Add(x => x.OrderTime, order.OrderDate.HasValue ? order.OrderDate.Value.ToString("hh:mm tt") : "");

        var dialog = await _dialogService.ShowAsync<ChoiceDriverDialog>(Localizer["Distribution_ChoiceDriver"], parameters);
        _commonProperties.ChoiceDriverDialogReference = dialog;
        var result = await dialog.Result;
        if (result != null && !result.Canceled)
        {
            UpdateDriverStatus();
            StateHasChanged();
        }
    }

    private void AutoDistribute()
    {
    }

    private void ClearAssignments()
    {
        showAssigned = false;
        Snackbar.Add("تم تفريغ جميع التعيينات", Severity.Warning);
        StateHasChanged();
    }

    private string GetButtonClass(bool isActive)
        => isActive ? "toggle-button-active" : "toggle-button-inactive";

    private bool IsVoidDisabled(OrderDto order)
    {
        // Check permission from OrderSettings (CanVoidFromBranch / CanVoidFromCallCenter)
        var deliverySettings = _commonProperties.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");
        if (deliverySettings != null)
        {
            if (_isCallCenter && deliverySettings.CanVoidFromCallCenter == false)
                return true;

            if (!_isCallCenter && deliverySettings.CanVoidFromBranch == false)
                return true;
        }
        else
        {
            // Fallback to old dispatcher-level setting if no OrderSettings found
            if (!_dynamicDispatcherSettings.AllowDeliveryVoidFromBranch)
                return true;
        }

        if (order.OrderState == "Dispatched" || order.OrderState == "Delivering")
            return true;

        if (_dynamicDispatcherSettings.AllowVoidLimitMinutesForDeliveryOrder && order.OrderDate.HasValue)
        {
            var timeDiff = DateTime.Now.TimeOfDay - order.OrderDate.Value.TimeOfDay;
            if (timeDiff.TotalMinutes > _dynamicDispatcherSettings.VoidLimitMinutesForDeliveryOrder)
                return true;
        }

        return false;
    }

    private async Task VoidOrder(OrderDto order)
    {
        var parameters = new DialogParameters<VoidOrderDialog>();
        parameters.Add(x => x.OrderId, order.Id);
        parameters.Add(x => x.IsDistribution, order.OrderState == "Assigned" ? false : true);
        parameters.Add(x => x.IsDineIn, false);

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = false };
        var dialog = await _dialogService.ShowAsync<VoidOrderDialog>(Localizer["Distribution_VoidOrderTitle"], parameters, options);
        var result = await dialog.Result;

        if (!result!.Canceled)
            await RefreshData();
    }

    private async Task ShowVoidedOrders()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.ExtraLarge, FullWidth = true, CloseButton = true };
        var parameters = new DialogParameters<VoidedOrdersDialog>();
        parameters.Add(x => x.DefaultOrderType, "Delivery");
        await _dialogService.ShowAsync<VoidedOrdersDialog>(Localizer["Distribution_VoidHistory"], parameters, options);
    }

    private async Task ViewOrder(OrderDto context)
    {
        var parameters = new DialogParameters<DeliveryOrderViewDialog>();
        parameters.Add("Order", context);
        parameters.Add(x => x.OnOrderCollected, EventCallback.Factory.Create<OrderDto>(this, OnOrderCollectedHandler));

        _commonProperties.DeliveryOrderViewDialogReference = await _dialogService.ShowAsync<DeliveryOrderViewDialog>(Localizer["Distribution_ViewOrder"], parameters);
    }

    private async Task ShowAvailableDrivers()
    {
        var parameters = new DialogParameters<DriversDialog>();
        parameters.Add(x => x.ShowAvailableDrivers, true);
        _commonProperties.DriversDialogReference = await _dialogService.ShowAsync<DriversDialog>(Localizer["Distribution_AvailableDrivers"], parameters);
    }

    private async Task ShowUnavailableDrivers()
    {
        var parameters = new DialogParameters<DriversDialog>();
        parameters.Add(x => x.ShowAvailableDrivers, false);
        _commonProperties.DriversDialogReference = await _dialogService.ShowAsync<DriversDialog>(Localizer["Distribution_UnavailableDrivers"], parameters);
    }

    private string GetRowClass(OrderDto order, int rowNumber)
    {
        if (order.OrderDate == null)
            return string.Empty;

        var orderTime = order.OrderDate.Value.TimeOfDay;
        var nowTime = DateTime.Now.TimeOfDay;

        var timeDiff = nowTime - orderTime;

        if (timeDiff.TotalMinutes < 0)
            timeDiff = timeDiff.Add(TimeSpan.FromDays(1));

        if (timeDiff.TotalMinutes >= _dynamicDispatcherSettings.CriticalTimeForDeliveryOrderPerMinute)
            return "bg-red-100";
        else if (timeDiff.TotalMinutes >= _dynamicDispatcherSettings.WarningTimeForDeliveryOrderPerMinute)
            return "bg-yellow-100";

        return string.Empty;
    }

    public async ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        foreach (var conn in _externalHubConnections)
        {
            try
            {
                if (conn.State == HubConnectionState.Connected || conn.State == HubConnectionState.Connecting)
                    await conn.StopAsync();

                await conn.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disposing external hub connection: {ex.Message}");
            }
        }
        _externalHubConnections.Clear();
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _ = DisposeAsync();
    }
}
