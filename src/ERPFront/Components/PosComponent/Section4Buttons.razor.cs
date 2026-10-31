namespace ERPFront.Components.PosComponent;

public partial class Section4Buttons
{
    private bool _isProcessing = false;
    private async Task PrintOrder()
    {
        if (_isProcessing) return;
        _isProcessing = true;
        try 
        {
            if (_commonProperties!.TableItems!.Any())
            {
                if (_commonProperties.CurrentPosMode == PosModes.TakeAway.ToString())
                {
                   var result = await _printOrderService.PrintTakeAwayOrder();
                    if(result is true)
                    {
                        _cartService.ClearTakeAwayOrderAttributes();
                        _cartService.UpdateFinanceSettingsByMode(_commonProperties.CurrentPosMode);
                        _services.NotifyStateChanged();
                    }
                }
               
                if (_commonProperties.CurrentPosMode == PosModes.DineIn.ToString())
                {
                    var result = PrintDineInOrder();
                    if (result is false)
                        return;

                    _cartService.ClearDineInOrderAttributes();
                }

                if (_commonProperties.CurrentPosMode == PosModes.Delivery.ToString())
                    await PrintDeliveryOrder();

                _commonProperties.CurrentPosMode = PosModes.TakeAway.ToString();
                _cartService.UpdateFinanceSettingsByMode(_commonProperties.CurrentPosMode);
                await _appDateService.UpdateOrderCount();
                await GetCurrentDayAndTime();
            }
            else
                _snackbar.Add("No Order to print", Severity.Info);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task GetCurrentDayAndTime()
    {
        var appDate = await _appDateService.GetAppDate();
        _commonProperties.PosDate = DateOnly.FromDateTime(appDate.PosDate);
        _commonProperties.CurrentOrderId = appDate.CurrentOrderNumber;
    }

    private async Task PrintDeliveryOrder()
    {
        var result = await _printOrderService.PrintDeliveryOrder();
        if (result)
        {
            var soundEnabled = _configuration.GetValue<bool?>("SoundEnableCallCenter") ?? false;
            if (soundEnabled)
            {
                await _jsRuntime.InvokeVoidAsync("playNotificationSound");
            }
            _snackbar.Add("Order Sent to Branch", Severity.Success);
        }
    }

    private Action? _stateChangedHandler;

    protected override void OnInitialized()
    {
        _stateChangedHandler = async () =>
        {
            try
            {
                await InvokeAsync(StateHasChanged);
            }
            catch (ObjectDisposedException) { }
            catch (Exception) { }
        };

        _services.OnChanged += () => _stateChangedHandler?.Invoke();
        _cartService.OnChange += () => _stateChangedHandler?.Invoke();
    }

    private void CancelOrder()
    {
        if (_commonProperties.CurrentPosMode == PosModes.DineIn.ToString())
            _cartService.ClearDineInOrderAttributes();
        else
            _cartService.ClearTakeAwayOrderAttributes();

        _cartService.CalculateSection4Table();
        _services.NotifyStateChanged();
    }

    public void Dispose()
    {
        if (_stateChangedHandler != null)
        {
            _services.OnChanged -= () => _stateChangedHandler?.Invoke();
            _cartService.OnChange -= () => _stateChangedHandler?.Invoke();
        }
    }

    private bool PrintDineInOrder()
    {
        List<TableItem>? appendedOrder = _commonProperties.AppendedTableItems;
        if (_commonProperties.TableItems?.Any() == true
            && appendedOrder?.Any() == false
            && _commonProperties.UpdateDineInOrder == true
            && _commonProperties.OrderDiscount is null)
        {
            _snackbar.Add("No Order Updated to print", Severity.Info);
            return false;
        }

        if (_commonProperties.OrderDiscount!.DiscountType is not null ||
            appendedOrder != null
            && appendedOrder.Any()
            )
        {
            UpdateExistingOrderItems(appendedOrder!);
            appendedOrder!.Clear();
        }
        else
            CreateTableOrder();

        return true;
    }

    private void UpdateExistingOrderItems(List<TableItem> appendedOrder)
    {
        DineInOrderDetails? existingOrder = CheckIfThereAreDineOrderAndReturnItIfExists();

        UpdateDineInOrderAppendNewItems(appendedOrder, existingOrder);
        UpdateOrderTotal(existingOrder);
        AddDineInOrderDiscountIfExists(existingOrder);
        CheckIsTableExist(existingOrder);
        RemoveItemsIfTableHasItems();
        ClearAppendData();
    }

    private void UpdateOrderTotal(DineInOrderDetails existingOrder)
     => existingOrder.BasicOrderDetails!.Total = _commonProperties.TotalAmountAfterDiscount;

    private void AddDineInOrderDiscountIfExists(DineInOrderDetails existingOrder)
    {
        if (existingOrder!.BasicOrderDetails!.OrderDiscount != _commonProperties.OrderDiscount)
            existingOrder.BasicOrderDetails.OrderDiscount = _commonProperties.OrderDiscount!;
    }

    private DineInOrderDetails CheckIfThereAreDineOrderAndReturnItIfExists()
    {
        var existingOrder = _commonProperties.GetActiveOrder();
        if (existingOrder == null)
        {
            existingOrder = FullTableOrderDetails();
            if(!_commonProperties.DineInOrdersDetails!.ContainsKey(_commonProperties.TableId))
                _commonProperties.DineInOrdersDetails[_commonProperties.TableId] = new List<DineInOrderDetails>();
            
            _commonProperties.DineInOrdersDetails[_commonProperties.TableId].Add(existingOrder);
        }

        return existingOrder;
    }

    private void UpdateDineInOrderAppendNewItems(List<TableItem> appendedOrder, DineInOrderDetails? existingOrder)
    {
        foreach (var newItem in appendedOrder)
        {
            TableItem? existingItem = existingOrder!.BasicOrderDetails!.Items
                .FirstOrDefault(item => item.Name == newItem.Name && AreAttributesEqual(item, newItem));

            if (existingItem != null)
            {
                existingItem.Quantity += newItem.Quantity;
                existingItem.Total = existingItem.Quantity * existingItem.Price;
            }
            else
                existingOrder.BasicOrderDetails!.Items.Add(newItem);
        }
    }

    private void ClearAppendData()
    {
        _commonProperties.UpdateDineInOrder = false;
        _commonProperties!.AppendedTableItems!.Clear();
    }

    private bool AreAttributesEqual(TableItem item1, TableItem item2)
    {
        if (item1.Attributes == null || item2.Attributes == null)
            return item1.Attributes == item2.Attributes;

        if (item1.Attributes.Count != item2.Attributes.Count)
            return false;

        return item1.Attributes.OrderBy(a => a.Id).ThenBy(a => a.Name)
               .SequenceEqual(item2.Attributes.OrderBy(a => a.Id).ThenBy(a => a.Name),
                              new AttributeDtoComparer());
    }

    private void CreateTableOrder()
    {
        var clonedOrder = FullTableOrderDetails();

        if(!_commonProperties.DineInOrdersDetails!.ContainsKey(_commonProperties.TableId))
            _commonProperties.DineInOrdersDetails[_commonProperties.TableId] = new List<DineInOrderDetails>();
        
        _commonProperties.DineInOrdersDetails[_commonProperties.TableId].Add(clonedOrder);

        CheckIsTableExist(clonedOrder);
        RemoveItemsIfTableHasItems();
    }

    private void RemoveItemsIfTableHasItems()
    {
        if (_commonProperties.TableItems?.Any() == true)
            _services.RemoveAllItems(_commonProperties.TableItems);
    }

    private void CheckIsTableExist(DineInOrderDetails clonedOrder)
    {
        if (clonedOrder.RelatedTableId != _commonProperties.TableId &&
                    !_commonProperties!.DineInOrdersDetails!.ContainsKey(clonedOrder.RelatedTableId ?? 0))
        {
            _commonProperties!.DineInOrdersDetails!.Add(clonedOrder.RelatedTableId ?? 0, new List<DineInOrderDetails> { clonedOrder });
        }
    }

    private DineInOrderDetails FullTableOrderDetails()
    {

        var clonedOrder = new DineInOrderDetails
        {
            CaptainId = _commonProperties!.CurrentDineInOrder!.CaptainId,
            CaptainName = _commonProperties.CurrentDineInOrder.CaptainName,
            RelatedTableId = _commonProperties.CurrentDineInOrder.RelatedTableId,
            RelatedTableName = _commonProperties.CurrentDineInOrder.RelatedTableName,
            BasicOrderDetails = new BlazorBase.Models.OrderDetails
            {
                OrderId = _commonProperties.CurrentOrderId,
                CashierName = _commonProperties.CurrentUser,
                OrderType = OrderTypes.DineIn.ToString(),
                OrderDataTime = _commonProperties.PosDate.HasValue
                    ? _commonProperties.PosDate.Value.ToDateTime(TimeOnly.FromTimeSpan(DateTime.Now.TimeOfDay))
                    : DateTime.Now,
                Items = _commonProperties.TableItems?.Select(item => item.Clone()).ToList() ?? new List<TableItem>(),
                Account = _commonProperties._financeSettingsList![0].Value ?? 0M,
                Total = _commonProperties.TotalAmountAfterDiscount,
                Service = _commonProperties!.CurrentDineInOrder!.BasicOrderDetails!.Service,
                Tax = _commonProperties.CurrentDineInOrder.BasicOrderDetails.Tax,
                OrderDiscount = _commonProperties!.OrderDiscount!,
            }
        };

        return clonedOrder;

    }

    private void AddOrderToWaitingQueue()
    {
        if (_commonProperties.CurrentPosMode == PosModes.TakeAway.ToString())
            _services.AddOrderToWaitingQueue(_commonProperties?.TableItems ?? []);
        else
            _snackbar.Add("Only TakeAway Orders Can Be Added To Waiting Queue", Severity.Info);
    }
}