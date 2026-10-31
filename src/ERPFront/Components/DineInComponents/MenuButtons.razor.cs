using BlazorBase.ERPFrontServices.DineInOrderServices;
using BlazorBase.ERPFrontServices.PrintOrderServices;

namespace ERPFront.Components.DineInComponents;

public partial class MenuButtons
{
    private void CreateDineInOrder()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            UpdateCurrentDineInOrder(orderDetails);
            _navigationManager.NavigateTo("/pos");
        }
        else
        {
            BackUpDineInOrder();

            if (_commonProperties!.CurrentDineInOrder!.CaptainName is null || _commonProperties.CurrentDineInOrder.RelatedTableName is null)
            {
                _snackbar.Add("Please Select Table and Captain", Severity.Error);
                return;
            }

            _navigationManager.NavigateTo("/pos");
        }
    }
    private void BackUpDineInOrder()
    {
        _commonProperties!.CurrentDineInOrder = new DineInOrderDetails
        {
            CaptainName = _commonProperties!.CurrentDineInOrder!.CaptainName,
            CaptainId = _commonProperties.CurrentDineInOrder.CaptainId,
            RelatedTableId = _commonProperties.CurrentDineInOrder.RelatedTableId,
            RelatedTableName = _commonProperties.CurrentDineInOrder.RelatedTableName,
            BasicOrderDetails = new BlazorBase.Models.OrderDetails
            {
                CashierName = _commonProperties.CurrentUser,
                Tax = _commonProperties!.DineInSettings!.Tax,
                Service = _commonProperties.DineInSettings.Service,
                Items = new List<TableItem>(),
                OrderDiscount = new()
            }
        };
    }
    private void BackToPos()
    {
        _navigationManager.NavigateTo("/pos");
        _commonProperties.AppendedTableItems!.Clear();
        _commonProperties.CurrentPosMode = PosModes.TakeAway.ToString();
    }
    private void UpdateCurrentDineInOrder(DineInOrderDetails dineInOrderDetails)
    {
        _commonProperties.UpdateDineInOrder = true;

        _commonProperties.TableItems = new List<TableItem>(
            dineInOrderDetails!.BasicOrderDetails!.Items.Select(item =>
            {
                var newItem = item.Clone();
                newItem.IsReadOnly = true;
                return newItem;
            })
        );
        _commonProperties.OrderDiscount = dineInOrderDetails.BasicOrderDetails.OrderDiscount;
        if (dineInOrderDetails.BasicOrderDetails.OrderDiscount.DiscountType == "percentage")
            _commonProperties!._financeSettingsList![1].Value = dineInOrderDetails.BasicOrderDetails.OrderDiscount.Percentage;
        else
            _commonProperties!._financeSettingsList![1].Value = dineInOrderDetails.BasicOrderDetails.OrderDiscount.Value;

        _commonProperties._financeSettingsList[4].Value = dineInOrderDetails.BasicOrderDetails.Total;
    }
 
    public void MergeTables(int primaryTableId, List<int> mergingTableIds)
    {
        if (!_commonProperties.DineInOrdersDetails!.ContainsKey(primaryTableId) || mergingTableIds.Count == 0)
            return;

        var primaryOrders = _commonProperties.DineInOrdersDetails[primaryTableId];
        var primaryOrder = primaryOrders.FirstOrDefault();
        if (primaryOrder == null) return;

        foreach (var tableId in mergingTableIds)
        {
            if (_commonProperties.DineInOrdersDetails.TryGetValue(tableId, out var mergingOrders))
            {
                foreach (var mergingOrder in mergingOrders)
                {
                    if (mergingOrder.BasicOrderDetails != null)
                    {
                        primaryOrder.BasicOrderDetails ??= new BlazorBase.Models.OrderDetails();
                        primaryOrder.BasicOrderDetails.Merge(mergingOrder.BasicOrderDetails);
                    }
                }
                _commonProperties.DineInOrdersDetails.Remove(tableId);
            }
        }
    }

    private async Task OpenMergeTablesDialog()
        => _commonProperties.DialogReference = await _dialogService.ShowAsync<MergeTables>("Merge Tables");

    private async Task TransferTable()
        => _commonProperties.DialogReference = await _dialogService.ShowAsync<TransferTable>("Transfer Table");

    [Inject] private IDineInOrderFrontService _dineInOrderService { get; set; } = default!;

    [Inject] private IPrintOrderService? _printOrderService { get; set; }

    private async Task PrintReceipt()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            if (_printOrderService != null)
            {
                await _printOrderService.PrintInitialDineInOrder(orderDetails);
                _snackbar.Add("Printing...", Severity.Info);
            }
            else
            {
                _snackbar.Add("Print service not available", Severity.Warning);
            }
        }
    }

    private async Task CloseTable()
    {
        var orderDetails = _commonProperties.GetActiveOrder();
        if (orderDetails != null)
        {
            var result = await _dineInOrderService.CloseDineInOrderAsync(orderDetails.DatabaseId);
            if (result)
            {
                var tableOrders = _commonProperties.DineInOrdersDetails![_commonProperties.TableId];
                tableOrders.Remove(orderDetails);
                if (!tableOrders.Any())
                {
                    _commonProperties.DineInOrdersDetails.Remove(_commonProperties.TableId);
                }
                
                _commonProperties.CurrentDineInOrder = null;
                _commonProperties.DineInOrderValues = new();
                _commonProperties.TableItems?.Clear();
                
                _snackbar.Add("Table closed", Severity.Success);
                _navigationManager.NavigateTo("/dineIn");
            }
        }
    }

    private async Task OpenSplitOrderDialog()
    {
        var activeOrder = _commonProperties.GetActiveOrder();
        if (activeOrder == null)
        {
            _snackbar.Add("No active order to split", Severity.Warning);
            return;
        }

        var parameters = new DialogParameters { ["OrderToSplit"] = activeOrder };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await _dialogService.ShowAsync<SplitOrderDialog>("Split Order", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            _navigationManager.NavigateTo("/dineIn", true);
        }
    }
}