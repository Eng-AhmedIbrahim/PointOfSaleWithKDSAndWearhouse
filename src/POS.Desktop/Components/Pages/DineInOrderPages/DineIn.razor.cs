namespace POS.Desktop.Components.Pages.DineInOrderPages;

using BlazorBase.Helpers;
using global::POS.Desktop.Components.DineInComponents;
using MudBlazor;

public partial class DineIn : IDisposable
{
    [Inject] private Section4ButtonsServices _services { get; set; } = default!;
    [Inject] private IDialogService _dialogService { get; set; } = default!;
    [Inject] private ISnackbar _snackbar { get; set; } = default!;

    private string GetCaptainModernClass(UserToReturnDto captain)
    {
        if (captain == null || string.IsNullOrEmpty(captain.Id))
            return "captain-modern-btn";

        var classes = new List<string> { "captain-modern-btn" };

        if (buttonStates != null &&
            buttonStates.TryGetValue(captain.Id, out bool isActive) &&
            isActive)
        {
            classes.Add("captain-active");
        }

        return string.Join(" ", classes);
    }

    public List<TableItem> Items { get; set; } = new();
    public List<TableGroupToReturnDto>? _tableGroups { get; set; }
    public List<TableToReturnDto>? _tables { get; set; }
    public List<UserToReturnDto>? _captainOrders { get; set; } = new();
    private Dictionary<string, bool> buttonStates = new();
    private Dictionary<int, bool> tableStates = new();
    private bool _isLoadingOrders = false;

    protected async override Task OnInitializedAsync()
    {
        // 1. Fire all tasks in parallel
        var tableGroupsTask = _dineInService.GetTableGroupsAsync();
        var captainOrdersTask = _dineInService.GetCaptainOrders();
        var openOrdersTask = _dineInOrderService.GetAllOpenDineInOrdersAsync();

        // 2. Wait for table groups and show them immediately
        var tableGroups = await tableGroupsTask;
        _tableGroups = tableGroups?.ToList() ?? new List<TableGroupToReturnDto>();
        
        if (_tableGroups.Any())
        {
            // Fetch tables for the first group and render ASAP
            await GetTablesFromGroup(_tableGroups.First().Id);
            await InvokeAsync(StateHasChanged);
        }

        // 3. Wait for captains and open orders (badges) in the background
        await Task.WhenAll(captainOrdersTask, openOrdersTask);
        
        _captainOrders = (await captainOrdersTask)?.ToList() ?? new List<UserToReturnDto>();
        
        var openOrders = await openOrdersTask;
        ProcessOpenOrders(openOrders);

        _commonProperties.AppendedTableItems?.Clear();
        _services.OnChanged += HandleStateChanged;
        
        await InvokeAsync(StateHasChanged);
    }

    private async void HandleStateChanged()
    {
        if (_isLoadingOrders) return;

        try 
        {
            _isLoadingOrders = true;
            await LoadOpenOrdersFromDatabase();
            
            if (_commonProperties == null) return;

            // Sync items with common properties state
            if (_commonProperties.CurrentDineInOrder == null)
            {
                Items = new List<TableItem>();
                // Also clear selection UI if no active table/order is set in common properties
                if (_commonProperties.TableId == 0 || (_commonProperties.DineInOrdersDetails != null && !_commonProperties.DineInOrdersDetails.ContainsKey(_commonProperties.TableId)))
                {
                    tableStates.Clear();
                    buttonStates.Clear();
                }
            }
            else
            {
                Items = _commonProperties.TableItems ?? new List<TableItem>();
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in DineIn HandleStateChanged");
        }
        finally
        {
            _isLoadingOrders = false;
        }
    }

    public void Dispose()
    {
        _services.OnChanged -= HandleStateChanged;
    }
    
    private async Task LoadOpenOrdersFromDatabase()
    {
        try
        {
            var openOrders = await _dineInOrderService.GetAllOpenDineInOrdersAsync();
            ProcessOpenOrders(openOrders);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading orders from database: {ex.Message}");
        }
    }

    private void ProcessOpenOrders(IEnumerable<DineInOrderDto>? openOrders)
    {
        if (_commonProperties == null) return;

        // Clear existing in-memory orders first to ensure synchronization
        _commonProperties.DineInOrdersDetails?.Clear();

        if (openOrders != null && openOrders.Any())
        {
            foreach (var dbOrder in openOrders)
            {
                var orderDetails = DineInOrderMapper.MapToDineInOrderDetails(dbOrder);
                if (_commonProperties.DineInOrdersDetails != null)
                {
                    if (!_commonProperties.DineInOrdersDetails.ContainsKey(dbOrder.TableId))
                    {
                        _commonProperties.DineInOrdersDetails[dbOrder.TableId] = new List<DineInOrderDetails>();
                    }
                    _commonProperties.DineInOrdersDetails[dbOrder.TableId].Add(orderDetails);
                }
            }
        }
    }

    private async Task GetTablesFromGroup(int tableGroupId)
    {
        var TableItems = await _dineInService.GetTablesByGroupId(tableGroupId);
        _tables = TableItems?.ToList() ?? new List<TableToReturnDto>();
    }

    private async Task GetCaptainOrders()
    {
        var CaptainOrders = await _dineInService.GetCaptainOrders();
        _captainOrders = CaptainOrders?.ToList() ?? new List<UserToReturnDto>();
    }

    private async Task ToggleState<T>(Dictionary<T, bool> stateDict, T key, string keyName, Action<Dictionary<T, bool>, T> stateUpdater) where T : notnull
    {
        if (key == null) return;

        if (key is int)
        {
            _commonProperties.TableId = Int32.Parse(key.ToString()!);
            
            var table = _tables?.FirstOrDefault(t => t.Id == _commonProperties.TableId);
            if (table?.TableState == "Reserved")
            {
                await ShowReservationDetails(table.Id!.Value);
                return;
            }

            if (_commonProperties.CurrentDineInOrder == null)
            {
                _commonProperties.CurrentDineInOrder = new DineInOrderDetails
                {
                    BasicOrderDetails = new BlazorBase.Models.OrderDetails
                    {
                        CashierName = _commonProperties.CurrentUser,
                        OrderDiscount = new()
                    }
                };
            }

            _commonProperties.CurrentDineInOrder.RelatedTableId = Int32.Parse(key.ToString()!);
            _commonProperties.CurrentDineInOrder.RelatedTableName = keyName;
            if (int.TryParse(key.ToString(), out int keyInt) && _commonProperties.DineInOrdersDetails?.TryGetValue(keyInt, out List<DineInOrderDetails>? orders) == true && orders.Any())
            {
                if (orders.Count > 1)
                {
                    await OpenMultipleOrdersDialog(orders);
                }
                else
                {
                    SetSelectedOrder(orders.First());
                }
            }
            else
            {
                Items.Clear();
                _commonProperties!.DineInOrderValues = new();
                _commonProperties.TableItems?.Clear();
                _commonProperties.AppendedTableItems?.Clear();
                _commonProperties.UpdateDineInOrder = false;
            }

        }
        if (key is string)
        {
            if (_commonProperties.CurrentDineInOrder != null)
            {
                _commonProperties.CurrentDineInOrder.CaptainId = key.ToString()!;
                _commonProperties.CurrentDineInOrder.CaptainName = keyName;
            }
            
            if (_commonProperties.DineInOrderValues != null)
            {
                // Also update DineInOrderValues so it shows in the UI card
                _commonProperties.DineInOrderValues.CaptainName = keyName;
            }
        }

        stateUpdater(stateDict, key);
        await InvokeAsync(StateHasChanged);
    }

    private void UpdateState<T>(Dictionary<T, bool> stateDict, T key) where T : notnull
    {
        foreach (var existingKey in stateDict.Keys.ToList())
        {
            stateDict[existingKey] = false;
        }

        stateDict[key] = true;
    }

    private string GetFloorClass(int id)
    {
        bool isActive = _tables != null && _tables.Any() && _tables.First().GroupID == id;
        return isActive ? "floor-btn floor-btn-active" : "floor-btn";
    }

    private string GetTableModernClass(TableToReturnDto table)
    {
        if (table == null || table.Id == null)
            return "table-modern-card";

        var classes = new List<string> { "table-modern-card" };

        if (_commonProperties?.DineInOrdersDetails != null && _commonProperties.DineInOrdersDetails.ContainsKey(table.Id.Value))
            classes.Add("table-card-has-order");
        else if (table.TableState == "Reserved")
            classes.Add("table-card-reserved");

        if (tableStates.TryGetValue(table.Id.Value, out bool isActive) && isActive)
            classes.Add("table-card-active");

        return string.Join(" ", classes);
    }

    private async Task OnTableRightClick(MouseEventArgs e, TableToReturnDto table)
    {
        if (table == null || table.Id == null) return;

        // Check if table is reserved
        var reservation = await _dineInOrderService.GetDineInOrderByTableIdAsync(table.Id.Value, "Reserved");
        
        if (reservation != null)
        {
            bool? cancelConfirm = await _dialogService.ShowMessageBox(
                Localizer["CancelReservation"],
                Localizer["ConfirmCancelReservation"],
                yesText: Localizer["Yes"], cancelText: Localizer["No"]);
            
            if (cancelConfirm == true)
            {
                var result = await _dineInOrderService.CancelReservationAsync(table.Id.Value);
                if (result)
                {
                    _snackbar.Add($"Reservation for {table.TableName} canceled", Severity.Success);
                    await GetTablesFromGroup(_tables?.FirstOrDefault()?.GroupID ?? 0);
                    StateHasChanged();
                }
                else
                {
                    _snackbar.Add("Failed to cancel reservation", Severity.Error);
                }
            }
        }
        else if (_commonProperties.DineInOrdersDetails == null || !_commonProperties.DineInOrdersDetails.ContainsKey(table.Id.Value))
        {
            if (!_commonProperties.IsFeatureEnabled("EnableReservation"))
            {
                _snackbar.Add(Localizer["ReservationSystemDisabled"], Severity.Info);
                return;
            }

            // Table is available - show reserve option
            var parameters = new DialogParameters 
            { 
                ["TableId"] = table.Id.Value,
                ["TableName"] = table.TableName,
                ["CurrentUser"] = _commonProperties.CurrentUser,
                ["CurrentUserId"] = _commonProperties.CurrentUserId,
                ["BranchId"] = _commonProperties.BranchDetails?.Id ?? 0
            };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            var dialog = await _dialogService.ShowAsync<TableReservationDialog>("Reserve Table", parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                await GetTablesFromGroup(_tables?.FirstOrDefault()?.GroupID ?? 0);
                StateHasChanged();
            }
        }
    }

    private void SetSelectedOrder(DineInOrderDetails orderDetails)
    {
        _commonProperties!.DineInOrderValues = new()
        {
            OrderID = orderDetails!.BasicOrderDetails!.OrderId,
            TableOpenTime = orderDetails?.BasicOrderDetails?.OrderDataTime?.ToString("hh:mm tt") ?? "N/A",
            TableName = orderDetails!.RelatedTableName,
            CaptainName = orderDetails.CaptainName,
            Total = orderDetails.BasicOrderDetails.Total > 0
                    ? orderDetails.BasicOrderDetails.Total
                    : (orderDetails.BasicOrderDetails.Items?.Sum(i => i.Total) ?? 0),
            Discount = (orderDetails.BasicOrderDetails.Account ?? 0) + 
                       (orderDetails.BasicOrderDetails.Service ?? 0) + 
                       (orderDetails.BasicOrderDetails.Tax ?? 0) - 
                       (orderDetails.BasicOrderDetails.Total ?? 0)
        };
        
        _commonProperties.TableItems = new List<TableItem>(
            (orderDetails?.BasicOrderDetails?.Items ?? new List<TableItem>()).Select(item =>
            {
                var newItem = item.Clone();
                newItem.IsReadOnly = true;
                return newItem;
            })
        );
        Items = _commonProperties.TableItems;
        _commonProperties.UpdateDineInOrder = true;
        _services.NotifyStateChanged();
    }

    private async Task OpenMultipleOrdersDialog(List<DineInOrderDetails> orders)
    {
        var parameters = new DialogParameters { ["Orders"] = orders };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.False, FullWidth = true };
        var dialog = await _dialogService.ShowAsync<MultipleOrdersDialog>("Select Order", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data != null)
        {
            var selectedOrder = result.Data as DineInOrderDetails;
            if (selectedOrder != null)
            {
                SetSelectedOrder(selectedOrder);
            }
        }
    }

    private async Task ShowReservationDetails(int tableId)
    {
        var reservation = await _dineInOrderService.GetDineInOrderByTableIdAsync(tableId, "Reserved");
        if (reservation != null)
        {
            var parameters = new DialogParameters 
            { 
                ["Reservation"] = reservation 
            };
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            var dialog = await _dialogService.ShowAsync<ReservationDetailsDialog>(Localizer["ReservationDetails"], parameters, options);
            var result = await dialog.Result;

            if (result != null && !result.Canceled)
            {
                await LoadOpenOrdersFromDatabase();
                
                // Auto-select the newly seated order
                if (_commonProperties.DineInOrdersDetails != null && 
                    _commonProperties.DineInOrdersDetails.TryGetValue(tableId, out var orders) && 
                    orders.Any())
                {
                    _commonProperties.CurrentDineInOrder = orders.First();
                    SetSelectedOrder(orders.First());
                    _commonProperties.TableId = tableId;
                    _commonProperties.CurrentDineInOrder!.RelatedTableName = reservation.TableName;
                }
                
                StateHasChanged();
            }
        }
    }
}