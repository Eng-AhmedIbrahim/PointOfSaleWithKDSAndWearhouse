using ERPFront.Components.DineInComponents;

namespace ERPFront.Components.Pages.DineInOrderPages;

public partial class DineIn
{
    public List<TableItem> Items { get; set; } = new();
    public List<TableGroupToReturnDto>? _tableGroups { get; set; }
    public List<TableToReturnDto>? _tables { get; set; }
    public List<UserToReturnDto>? _captainOrders { get; set; } = new();
    private Dictionary<string, bool> buttonStates = new();
    private Dictionary<int, bool> tableStates = new();
    protected async override Task OnInitializedAsync()
    {
        var TableGroups = await _DineInService.GetTableGroupsAsync();
        _tableGroups = TableGroups.ToList();


        _commonProperties.AppendedTableItems!.Clear();

        await GetCaptainOrders();
    }

    private async Task GetTablesFromGroup(int tableGroupId)
    {
        var TableItems = await _DineInService.GetTablesByGroupId(tableGroupId);
        _tables = TableItems.ToList();
    }

    private async Task GetCaptainOrders()
    {
        var CaptainOrders = await _DineInService.GetCaptainOrders();
        _captainOrders = CaptainOrders.ToList();
    }

    private async Task ToggleState<T>(Dictionary<T, bool> stateDict, T key, string keyName, Action<Dictionary<T, bool>, T> stateUpdater) where T : notnull
    {
        if (key == null) return;

        if (key is int)
        {
            _commonProperties.TableId = Int32.Parse(key.ToString()!);
            
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

    private string GetTableClass(TableToReturnDto table)
    {
        if (table == null || table.Id == null)
            return "order-button";

        _commonProperties!.DineInOrdersDetails!.TryGetValue(table.Id.Value, out var found);
        if (found is null)
        {
            return tableStates.TryGetValue(table.Id.Value, out bool isActive) && isActive
                ? "order-button red-button"
                : "order-button";
        }

        return "order-button";
    }

    [Inject] private Section4ButtonsServices _services { get; set; } = default!;
    [Inject] private IDialogService _dialogService { get; set; } = default!;

    private string GetButtonClass(UserToReturnDto captainOrder)
    {
        if (captainOrder == null || string.IsNullOrEmpty(captainOrder.Id))
            return "order-button";

        return buttonStates.TryGetValue(captainOrder.Id, out bool isActive) && isActive
            ? "order-button red-button"
            : "order-button";
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
                    : (orderDetails.BasicOrderDetails.Items?.Sum(i => i.Total) ?? 0)
        };

        _commonProperties.TableItems = new List<TableItem>(
            orderDetails!.BasicOrderDetails!.Items?.Select(item =>
            {
                var newItem = item.Clone();
                newItem.IsReadOnly = true;
                return newItem;
            }).ToList() ?? new List<TableItem>()
        );
        Items = _commonProperties.TableItems;
        _commonProperties.UpdateDineInOrder = true;
        _services.NotifyStateChanged();
    }

    private async Task OpenMultipleOrdersDialog(List<DineInOrderDetails> orders)
    {
        var parameters = new DialogParameters { ["Orders"] = orders };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await _dialogService.ShowAsync<MultipleOrdersDialog>("Select Order", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is DineInOrderDetails selectedOrder)
        {
            SetSelectedOrder(selectedOrder);
        }
    }
}