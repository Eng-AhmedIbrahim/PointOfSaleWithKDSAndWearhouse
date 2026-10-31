namespace ERPFront.Components.Pages;

public partial class POS
{
    private Dictionary<int, int> _itemClickCount = new Dictionary<int, int>();
    private MenuSalesItemsToReturnDto? _currentBaseItem;
    private ICollection<CategoryToReturnDto>? _categories = new List<CategoryToReturnDto>();
    private ICollection<MenuSalesItemsToReturnDto> _itemByCatId = new List<MenuSalesItemsToReturnDto>();
    private int currentCatId;
    private List<AttributeDto>? currentSelectedAttribute;
    private delegate void FinanceSettingsDelegate(OrderSettingToReturnDto? orderSettings);
    private DotNetObjectReference<POS>? _dotNetRef;
    public string? NoteValue { get; set; }
    private double _spacing = 4.0;


    protected override async Task OnInitializedAsync()
    {
        _categories = await _categoryServices.GetAllCategoriesAsync();
        _categories = _categories.Where(c => c.Invisible == false).ToList();

        _commonProperties.OnChange += StateHasChanged;
        CustomizationSettingsService.OnChanged += StateHasChanged;
        _section4ButtonsServices.OnChanged += () => InvokeAsync(StateHasChanged);

        if (string.IsNullOrEmpty(_commonProperties.CustomerDetails!.FirstPhoneNumber))
            _handelDeliveryInvocation.DeliveryDetails = string.Empty;

        await GetCurrentDayAndTime();
        await GetOrdersSetting();

        if (!_commonProperties.UpdateDineInOrder)
            _cartService.ResetDiscount();


        _cartService.UpdateFinanceSettingsByMode(_commonProperties.CurrentPosMode);

        if (_commonProperties!.TableItems!.Any())
        {
            CalculateTotalAmount();
            _cartService.CalculateSection4Table();
        }
        else
        {
            _commonProperties.TotalDiscount = 0M;
            _commonProperties!._financeSettingsList![1].Value = 0M;
        }


        _commonProperties.BranchDetails = await GetBranchDetails();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await CustomizationSettingsService.LoadMaxHeights();
            _dotNetRef = DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("listenToDoubleEnter", _dotNetRef);
            StateHasChanged();
        }
    }
    private async Task InvokeItems(int catId)
    {
        _itemByCatId = await _categoryServices.GetItemsByCategoryIdAsync(catId);
        _itemByCatId = _itemByCatId.Where(i => i.Invisible == false).ToList();

        currentCatId = catId;
    }

    private Task OnSection4ItemsChanged()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task AddItemToSection4(MenuSalesItemsToReturnDto selectedMenuItem)
    {
        // If we are currently selecting attributes for a base item, this click is an attribute choice!
        // We must NOT treat it as a standalone menu item (which would check inventory, cashed items, etc.)
        if (_currentBaseItem != null && (selectedMenuItem.Attributes == null || !selectedMenuItem.Attributes.Any()))
        {
            await ProcessAttributeSelection(selectedMenuItem);
            UpdateOrderTotals();
            return;
        }

        TableItem? existingCashedItem = GetItemFromTableById(selectedMenuItem);

        if (IsCashedItem(existingCashedItem))
        {
            HandleCashedItem(selectedMenuItem, existingCashedItem!);
        }
        else
        {
            await HandleNewItemSelection(selectedMenuItem, existingCashedItem);
        }

        UpdateOrderTotals();
    }

    /// <summary>
    /// Checks if the item is a cashed (read-only) order.
    /// </summary>
    private bool IsCashedItem(TableItem? item) => item != null && item.IsReadOnly;

    /// <summary>
    /// Handles adding a new item when the selected item is a cashed order.
    /// </summary>
    private void HandleCashedItem(MenuSalesItemsToReturnDto selectedMenuItem, TableItem existingCashedItem)
    {
        TableItem? existingNewItem = _commonProperties!.TableItems!
            .FirstOrDefault(item => item.Name == existingCashedItem.Name && !item.IsReadOnly);

        if (existingNewItem != null)
        {
            IncrementExistingNewItem(existingNewItem);
        }
        else
        {
            AddNewEditableItem(existingCashedItem);
        }
    }

    /// <summary>
    /// Increments the quantity and total price of an existing new item.
    /// </summary>
    private void IncrementExistingNewItem(TableItem existingNewItem)
    {
        existingNewItem.Quantity++;
        _cartService.RecalculateItemTotals(existingNewItem);
    }

    /// <summary>
    /// Creates and adds a new editable version of a cashed item.
    /// </summary>
    private void AddNewEditableItem(TableItem existingCashedItem)
    {
        TableItem newItem = new TableItem
        {
            Name = existingCashedItem.Name,
            Price = existingCashedItem.Price,
            Quantity = 1,
            IsReadOnly = false,
            Attributes = existingCashedItem.Attributes?.Select(attr => attr.Clone()).ToList() ?? new List<AttributeDto>()
        };
        _cartService.RecalculateItemTotals(newItem);

        if (_commonProperties!.UpdateDineInOrder)
            _commonProperties.AppendedTableItems!.Add(newItem);

        _commonProperties!.TableItems!.Add(newItem);
    }


    /// <summary>
    /// Handles adding a new item when the selected item is not a cashed order.
    /// </summary>
    private async Task HandleNewItemSelection(MenuSalesItemsToReturnDto selectedMenuItem, TableItem? existingItem)
    {
        if (!selectedMenuItem.Attributes.Any() && _itemClickCount.Count() == 0 && existingItem != null)
            IncrementExistingNewItem(existingItem);
        else
            await ProcessAttributeSelection(selectedMenuItem);
    }

    /// <summary>
    /// Processes attribute selection logic for new items.
    /// </summary>
    private async Task ProcessAttributeSelection(MenuSalesItemsToReturnDto selectedMenuItem)
    {
        if (_itemClickCount.Count == 0)
            InitializeBaseItem(selectedMenuItem);

        var currentClickCount = GetCurrentClickCount();

        if (currentClickCount < _currentBaseItem?.Attributes.Count)
            ProcessAttributeClick(selectedMenuItem, currentClickCount);
        else
            await FinalizeAttributeSelection(selectedMenuItem);
    }

    /// <summary>
    /// Handles attribute selection process.
    /// </summary>
    private void ProcessAttributeClick(MenuSalesItemsToReturnDto selectedMenuItem, int currentClickCount)
    {
        if (currentClickCount > 0)
            AddAttributeNameToSection4Item(selectedMenuItem, currentClickCount);

        UpdateAttributeGroup(currentClickCount);
        IncrementClickCount();
    }

    /// <summary>
    /// Finalizes the attribute selection and adds the item to the table.
    /// </summary>

    private async Task FinalizeAttributeSelection(MenuSalesItemsToReturnDto selectedMenuItem)
    {
        if (_currentBaseItem != null && _currentBaseItem.Attributes.Any())
            AddAttributeNameToSection4Item(selectedMenuItem, GetCurrentClickCount());

        var itemToAdd = _currentBaseItem;

        ResetClickCountAndBaseItem();

        if (itemToAdd != null)
        {
            await AddItemToTable(itemToAdd);
            await UpdateSectionWithInitialItems();
        }
    }

    private async Task UpdateSectionWithInitialItems()
    {
        var initialItems = await GetInitialMenuItems();

        _itemByCatId = new List<MenuSalesItemsToReturnDto>(initialItems);

        StateHasChanged();
    }

    private async Task<IEnumerable<MenuSalesItemsToReturnDto>> GetInitialMenuItems()
    {
        var initialItems = await _categoryServices.GetItemsByCategoryIdAsync(currentCatId);

        return initialItems.Where(i => !i.Invisible);
    }
    /// <summary>
    /// Updates order totals and UI.
    /// </summary>
    private void UpdateOrderTotals()
    {
        UpdateOrderTotal();
        UpdateTableItemCount();
        StateHasChanged();
    }

    private void UpdateOrderTotal()
    {
        _commonProperties!.TotalOrderPrice = _commonProperties!.TableItems!
            .Where(item => !item.IsReadOnly)
            .Sum(item => item.Total);

        _cartService.CalculateTotalAmountFromTableItems(_commonProperties!.TableItems!);
        _cartService.CalculateSection4Table();
    }

    private void AddAttributeNameToSection4Item(MenuSalesItemsToReturnDto selectedMenuItem, int currentClickCount)
    {
        if (selectedMenuItem == null) return;

        var newAttribute = new AttributeDto
        {
            Id = selectedMenuItem.Id,
            Name = selectedMenuItem.ArabicName ?? string.Empty,
            ExtraPrice = selectedMenuItem.ExtraPrice
        };

        currentSelectedAttribute?.Add(newAttribute);
        _currentBaseItem!.Price += selectedMenuItem.ExtraPrice ?? 0;
    }

    private void InitializeBaseItem(MenuSalesItemsToReturnDto menuItem)
    {
        _currentBaseItem = menuItem;
        _itemClickCount[menuItem.Id] = 0;
        currentSelectedAttribute = [];
    }

    private int GetCurrentClickCount()
     => _itemClickCount[_currentBaseItem?.Id ?? 0];

    private void UpdateAttributeGroup(int clickCount)
    {
        MenuSalesItemAttributes? attributeGroup = _currentBaseItem?.Attributes
            .FirstOrDefault(a => a.AppearanceIndex == clickCount + 1);

        if (attributeGroup != null)
        {
            _itemByCatId.Clear();

            foreach (MenuSalesItemsGroupDto item in attributeGroup.GroupItems)
            {
                var newMenuItem = new MenuSalesItemsToReturnDto
                {
                    Id = item.Id,
                    ArabicName = item.ArabicName,
                    EnglishName = item.EnglishName,
                    Price = item.Price,
                    ExtraPrice = item.ExtraPrice ?? 0
                };

                _itemByCatId.Add(newMenuItem);
            }
        }
    }

    private void IncrementClickCount()
         => _itemClickCount[_currentBaseItem?.Id ?? 0]++;

    private async Task AddItemToTable(MenuSalesItemsToReturnDto menuItem)
    {
        TableItem? newTableItem = new TableItem
        {
            Id = menuItem.Id,
            Name = menuItem.ArabicName,
            Price = menuItem.Price ?? 0,
            Quantity = 1,
            Attributes = currentSelectedAttribute ?? [],
            CategoryKitchenTypeId = menuItem.CategoryKitchenTypeId,
            ItemKitchenTypeId = menuItem.KitchenTypeId,
            PrintInBackupReceiptFromCategory = menuItem.PrintInBackupReceiptFromCategory,
            PrintInBackupReceiptFromItem = menuItem.PrintInBackupReceipt,
            ExtraPrice = menuItem.ExtraPrice
        };
        _cartService.RecalculateItemTotals(newTableItem);
        _commonProperties?.TableItems?.Add(newTableItem);

        if (_commonProperties!.UpdateDineInOrder)
            _commonProperties!.AppendedTableItems!.Add(newTableItem);

        CalculateTotalAmount();

        _cartService.CalculateSection4Table();

        await InvokeItems(currentCatId);
    }

    private void ResetClickCountAndBaseItem()
    {
        _itemClickCount.Clear();
        _currentBaseItem = null;
    }
    private void UpdateTableItemCount()
    {
        int count = _commonProperties?.TableItems?.Count ?? 0;
        JsRuntime.InvokeVoidAsync("setTableItemCount", count);
    }

    public void ClearTableItems()
    {
        _commonProperties?.TableItems?.Clear();
        CalculateTotalAmount();
        StateHasChanged();
    }

    private TableItem? GetItemFromTableById(MenuSalesItemsToReturnDto selectedMenuItem)
        => _commonProperties?.TableItems?.Where(c => c!.Attributes!.Count == 0).FirstOrDefault(s => s.Id == selectedMenuItem.Id);

    public void CalculateTotalAmount()
    {
        if (_commonProperties?.TableItems != null)
        {
            _commonProperties._financeSettingsList![0].Value = _commonProperties.TableItems.Sum(i => i.Total ?? 0);
            StateHasChanged();
        }
    }
    private async Task GetCurrentDayAndTime()
    {
        var appDate = await _appDate.GetAppDate();
        _commonProperties.PosDate = DateOnly.FromDateTime(appDate.PosDate);
        _commonProperties.CurrentOrderId = appDate.CurrentOrderNumber;
    }

    private async Task GetOrdersSetting()
    {
        var orderSettingToReturnDtos = await _orderSettingsService.GetOrderSettingsAsync();

        _commonProperties.OrderSettings = orderSettingToReturnDtos!;

        foreach (var item in orderSettingToReturnDtos!)
        {
            switch (item.OrderType)
            {
                case "DineIn":
                    _commonProperties.DineInSettings = new()
                    {
                        OrderStatment = item.OrderStatment,
                        JobID = item.JobID,
                        Service = item.Service,
                        Tax = item.Tax,
                        Tips = item.Tips,
                        SeparateReceiptCount = item.SeparateReceiptCount,
                        AddServiceToItemPrice = item.AddServiceToItemPrice,
                        ClosingReceiptCount = item.ClosingReceiptCount,
                        CustomerReceiptCount = item.CustomerReceiptCount,
                        FullKitchenReceiptCount = item.FullKitchenReceiptCount
                    };
                    break;
                case "TakeAway":
                    _commonProperties.TakeAwaySettings = new()
                    {
                        OrderStatment = item.OrderStatment,
                        JobID = item.JobID,
                        Service = item.Service,
                        Tax = item.Tax,
                        Tips = item.Tips,
                        SeparateReceiptCount = item.SeparateReceiptCount,
                        AddServiceToItemPrice = item.AddServiceToItemPrice,
                        ClosingReceiptCount = item.ClosingReceiptCount,
                        CustomerReceiptCount = item.CustomerReceiptCount,
                        FullKitchenReceiptCount = item.FullKitchenReceiptCount
                    };
                    break;
                case "Delivery":
                    _commonProperties.DeliverySettings = new()
                    {
                        OrderStatment = item.OrderStatment,
                        JobID = item.JobID,
                        Service = item.Service,
                        Tax = item.Tax,
                        Tips = item.Tips,
                        SeparateReceiptCount = item.SeparateReceiptCount,
                        AddServiceToItemPrice = item.AddServiceToItemPrice,
                        ClosingReceiptCount = item.ClosingReceiptCount,
                        CustomerReceiptCount = item.CustomerReceiptCount,
                        FullKitchenReceiptCount = item.FullKitchenReceiptCount
                    };
                    break;
                default:
                    break;
            }
        }
    }
    public void Dispose()
    {
        _commonProperties.OnChange -= StateHasChanged;
        CustomizationSettingsService.OnChanged -= StateHasChanged;
        _section4ButtonsServices.OnChanged -= StateHasChanged;
    }

    [JSInvokable]
    public async Task ExecuteDoubleEnterFunction()
    {
        switch (_commonProperties.CurrentPosMode)
        {
            case "DineIn":
                await CreateDineInOrder();
                break;
            case "Delivery":
                await CreateDeliveryOrder();
                break;
            case "TakeAway":
                await CreateTakeawayOrder();
                break;
            default:
                break;
        }
    }
    private async Task CreateDeliveryOrder()
    {
        throw new NotImplementedException();
    }

    private async Task CreateDineInOrder()
    {
        throw new NotImplementedException();
    }

    private async Task CreateTakeawayOrder()
    {
        if (_commonProperties.TableItems!.Count == 0)
            return;

        var result = await _printOrderService.PrintTakeAwayOrder();
        if (result is true)
        {
            _cartService.ClearTakeAwayOrderAttributes();
            _cartService.UpdateFinanceSettingsByMode(_commonProperties.CurrentPosMode);
            await _appDate.UpdateOrderCount();
            await GetCurrentDayAndTime();
            _services.NotifyStateChanged();
        }
    }

    private async Task<BranchToReturnDto> GetBranchDetails()
    {
        var branches = await _branchService.GetBranches();
        var Branch = branches.FirstOrDefault(b => b.Id == 1);
        if (Branch == null)
            return new BranchToReturnDto();

        return Branch!;
    }
}