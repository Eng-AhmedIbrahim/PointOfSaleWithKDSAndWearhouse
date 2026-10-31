namespace BlazorBase.ERPFrontServices.Section4ButtonsService;

public class Section4ButtonsServices : ISection4ButtonsServices
{
    public event Action? OnChanged;
    public event Func<Task>? OnPrintRequested;
    private readonly CartService? _cartService;
    private readonly CommonProperties _commonProperties;
    private int _nextOrderId = 1;

    public Section4ButtonsServices(CartService cartService, CommonProperties commonProperties)
    {
        _cartService = cartService;
        _commonProperties = commonProperties;
    }

    public void AddOrderToWaitingQueue(List<TableItem> tableItems)
    {
        if (tableItems.Any())
        {
            if (_commonProperties!.WaitingQueue!.WaitingOrders.Count == 0)
            {
                _nextOrderId = 1;
            }

            _commonProperties.WaitingQueue.WaitingOrders.Add(new()
            {
                Id = _nextOrderId++,
                Items = new List<TableItem>(tableItems),
            });

            RemoveAllItems(tableItems);
        }
    }
     public void RemoveAllItems(List<TableItem> tableItems)
    {
        tableItems.Clear();
        
        _commonProperties!._financeSettingsList![1].Value = 0M;
        _commonProperties.OrderDiscount = new();
        _commonProperties.TotalDiscount = 0M;

        _cartService!.CalculateTotalAmountFromTableItems(new ());
        _cartService.CalculateSection4Table();
        _cartService.ResetDiscount();
        
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnChanged?.Invoke();

    public void TriggerPrint()
    {
        if (OnPrintRequested != null)
        {
            foreach (var handler in OnPrintRequested.GetInvocationList().Cast<Func<Task>>())
            {
                _ = handler.Invoke();
            }
        }
    }
}