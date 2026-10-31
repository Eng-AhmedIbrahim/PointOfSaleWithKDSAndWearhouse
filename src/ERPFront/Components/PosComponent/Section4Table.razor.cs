namespace ERPFront.Components.PosComponent;

public partial class Section4Table
{
    public TableItem? _elementBeforeEdit;

    [Parameter] public List<TableItem>? Items { get; set; }
    [Parameter] public EventCallback OnItemsChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool IsReadOnly { get; set; } = false; // New Parameter
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        Snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
        _cartService.OnChange += HandleCartServiceChange;
    }

    private async void HandleCartServiceChange()
    => await InvokeAsync(StateHasChanged);

    public void Dispose()
    => _cartService.OnChange -= HandleCartServiceChange;

    private async void BackupItem(object element)
    {
        if (IsReadOnly) return;

        var item = (TableItem)element;
        _elementBeforeEdit = new TableItem
        {
            Id = item.Id,
            Quantity = item.Quantity,
            Name = item.Name,
            Price = item.Price,
            Total = item.Total
        };

        Snackbar.Add("Backup Item Handler Invoked");

        await Js.InvokeVoidAsync("focusRow", $"row-{item.Name}");
    }

    private void ResetItemToOriginalValues(object element)
    {
        if (_elementBeforeEdit != null)
        {
            var item = (TableItem)element;
            item.Quantity = _elementBeforeEdit.Quantity;
            item.Name = _elementBeforeEdit.Name;
            item.Price = _elementBeforeEdit.Price;
            item.Total = _elementBeforeEdit.Total;
        }

        Snackbar.Add("Reset Item Handler Invoked");
    }

    private void ItemHasBeenCommitted(object element)
    {
        ((TableItem)element).Total =
            ((TableItem)element).Quantity * ((TableItem)element).Price;

        Snackbar.Add("Item Has Been Committed Handler Invoked");
    }

    private void UpdateQuantity(TableItem item, decimal newQuantity)
    {
        item.Quantity = newQuantity;
        item.Total = item.Quantity * item.Price;
        item.TotalAmount = item.Quantity * item.Price;
        _cartService.CalculateTotalAmountFromTableItems(CommonProperties!.TableItems ?? []);
        _cartService.CalculateSection4Table();
    }


    private void SelectItem(TableItem item)
     => _cartService.SetSelectedItem(item);
}