namespace ERPFront.Components.Pages;

public partial class WaitingPage
{
    public int CurrentOrderId { get; set; }
    public List<TableItem>? Items { get; set; }
    private void ShowWaitingOrder(int orderId)
    {
        CurrentOrderId = orderId;
        Items = _commonProperties!.WaitingQueue!.WaitingOrders
        .Where(o => o.Id == orderId)
        .Select(o => o.Items.Select(item =>
        {
            var newItem = item.Clone();
            newItem.IsReadOnly = true;
            return newItem;
        }).ToList())
        .FirstOrDefault() ?? new();
    }

    private Task OnWaitingItemsChanged()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void RemoveWaitingOrder()
    {
        var orderToRemove = _commonProperties!.WaitingQueue!.WaitingOrders.FirstOrDefault(o => o.Id == CurrentOrderId);
        if (orderToRemove != null)
        {
            _commonProperties.WaitingQueue.WaitingOrders.Remove(orderToRemove);
        }

        Items = new List<TableItem>();

        StateHasChanged();
    }

    private void CompleteWaitingOrder()
    {
        WaitingOrder? waitingOrder = _commonProperties!.WaitingQueue!.WaitingOrders.FirstOrDefault(o => o.Id == CurrentOrderId);
        _commonProperties.TableItems = waitingOrder!.Items;
        _commonProperties!._financeSettingsList![0].Value = _commonProperties.TableItems.Sum(i => i.Total ?? 0);

        RemoveWaitingOrder();
        _navigationManager.NavigateTo("/pos");
    }
}