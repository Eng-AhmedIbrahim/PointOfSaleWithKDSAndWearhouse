namespace POS.Desktop.Components.PosComponent;

public partial class Section4Table
{
    public TableItem? _elementBeforeEdit;

    [Parameter] public List<TableItem>? Items { get; set; }
    [Parameter] public EventCallback OnItemsChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool IsReadOnly { get; set; } = false; // New Parameter
    
    [Inject] public IInventoryFrontService InventoryService { get; set; } = default!;
    [Inject] public IPosFeatureSettingsService FeatureSettingsService { get; set; } = default!;
    [Inject] public CommonProperties _commonProperties { get; set; } = default!;
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
        var item = (TableItem)element;
        _cartService.RecalculateItemTotals(item);
        _cartService.CalculateSection4Table();

        Snackbar.Add("Item Has Been Committed Handler Invoked");
    }

    private async void UpdateQuantity(TableItem item, decimal newQuantity)
    {
        if (newQuantity < 0) newQuantity = 0;
        if (newQuantity > item.Quantity)
        {
            // Check inventory if increasing
            bool isStopEnabled = await FeatureSettingsService.IsFeatureEnabledAsync("EnableMinQuantityStop", Environment.MachineName);
            if (isStopEnabled)
            {
                var response = await InventoryService.GetInventoryByItemIdAsync(item.Id);
                if (response.Success && response.Data != null)
                {
                    var inventory = response.Data;
                    if (inventory.TrackInventory)
                    {
                        decimal otherCartQty = Items?
                            .Where(i => i.Id == item.Id && i != item && !i.IsReadOnly && !i.IsVoided)
                            .Sum(i => i.Quantity) ?? 0;

                        if (inventory.CurrentQuantity - (otherCartQty + newQuantity) < inventory.MinimumQuantity)
                        {
                            decimal availableForThisLine = inventory.CurrentQuantity - inventory.MinimumQuantity - otherCartQty;
                            if (availableForThisLine < 0) availableForThisLine = 0;

                            string msg = Localizer.GetCurrentLanguage() == "ar"
                                ? $"الكمية المطلوبة ({newQuantity}) غير متوفرة. المتاح لهذا الصنف: {availableForThisLine:0.##}"
                                : $"Quantity ({newQuantity}) not available. Available: {availableForThisLine:0.##}";

                            Snackbar.Add(msg, Severity.Error);
                            await InvokeAsync(StateHasChanged);
                            return;
                        }
                    }
                }
            }

            // Check Staff Meal budget if increasing
            if (_commonProperties.CurrentStaffMeal != null)
            {
                bool isAmountMode = _commonProperties.CurrentStaffMeal.DailyAmountLimit > 0;
                decimal qtyDiff = newQuantity - item.Quantity;

                if (!isAmountMode)
                {
                    // COUNT MODE: check MealLimit
                    var mealLimit = _commonProperties.CurrentStaffMeal.MealLimit;
                    decimal totalCartQty = Items?.Where(i => !i.IsVoided).Sum(i => i.Quantity) ?? 0;
                    if (totalCartQty + qtyDiff > mealLimit)
                    {
                        Snackbar.Add(Localizer.GetCurrentLanguage() == "ar"
                            ? $"تخطيت الحد الأقصى للوجبة ({mealLimit})"
                            : $"Meal limit reached ({mealLimit})", Severity.Error);
                        await InvokeAsync(StateHasChanged);
                        return;
                    }
                }
                else
                {
                    // AMOUNT MODE: check monetary budget only
                    decimal itemOriginalPrice = item.OriginalPrice ?? 0;
                    decimal currentSpentInOrder = Items?.Where(i => !i.IsVoided).Sum(i => (i.OriginalPrice ?? 0) * i.Quantity) ?? 0;
                    if (currentSpentInOrder + (itemOriginalPrice * qtyDiff) > _commonProperties.RemainingStaffMealAmount)
                    {
                        Snackbar.Add(Localizer.GetCurrentLanguage() == "ar" ? "الرصيد المتبقي غير كافي" : "Insufficient balance", Severity.Error);
                        await InvokeAsync(StateHasChanged);
                        return;
                    }
                }
            }
        }

        item.Quantity = newQuantity;
        _cartService.RecalculateItemTotals(item);
        _cartService.CalculateSection4Table();
    }

    private async void UpdateWeightQty(TableItem item, decimal newWeightKg)
    {
        if (newWeightKg <= 0) return;

        if (newWeightKg > (item.WeightQty ?? item.Quantity))
        {
             // Check inventory if increasing weight
             bool isStopEnabled = await FeatureSettingsService.IsFeatureEnabledAsync("EnableMinQuantityStop", Environment.MachineName);
             if (isStopEnabled)
             {
                 var response = await InventoryService.GetInventoryByItemIdAsync(item.Id);
                 if (response.Success && response.Data != null)
                 {
                     var inventory = response.Data;
                     if (inventory.TrackInventory)
                     {
                         decimal otherCartQty = Items?
                             .Where(i => i.Id == item.Id && i != item && !i.IsReadOnly && !i.IsVoided)
                             .Sum(i => i.Quantity) ?? 0;

                         if (inventory.CurrentQuantity - (otherCartQty + newWeightKg) < inventory.MinimumQuantity)
                         {
                             decimal availableForThisLine = inventory.CurrentQuantity - inventory.MinimumQuantity - otherCartQty;
                             if (availableForThisLine < 0) availableForThisLine = 0;

                             string msg = Localizer.GetCurrentLanguage() == "ar"
                                 ? $"الوزن المطلوب ({newWeightKg}) غير متوفر. المتاح: {availableForThisLine:0.###} كجم"
                                 : $"Weight ({newWeightKg}) not available. Available: {availableForThisLine:0.###} kg";
                             
                             Snackbar.Add(msg, Severity.Error);
                             await InvokeAsync(StateHasChanged);
                             return;
                         }
                     }
                 }
             }
        }

        item.WeightQty = newWeightKg;
        item.Quantity = newWeightKg;

        _cartService.RecalculateItemTotals(item);
        _cartService.CalculateSection4Table();
        await InvokeAsync(StateHasChanged);
    }

    private void SelectItem(TableItem item)
     => _cartService.SetSelectedItem(item);
}
