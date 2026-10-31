namespace BlazorBase.ERPFrontServices.CartServices;

public class CartService : ICartService
{
    public TableItem? SelectedItem { get; private set; }
    public event Action? OnChange;
    private readonly CommonProperties? _commonProperties;

    public CartService(CommonProperties commonProperties)
        => _commonProperties = commonProperties;

    public void SetSelectedItem(TableItem item)
       => SelectedItem = item;

    public void AppendNumberToQuantity(string number)
    => UpdateQuantity(current =>
    {
        string currentStr = current.ToString();
        string newStr = (current == 0 || current == 1) ? number : currentStr + number;
        return decimal.TryParse(newStr, out decimal result) ? result : current;
    });

    public void OnClickBS()
     => UpdateQuantity(current =>
     {
         string currentStr = current.ToString();
         if (currentStr.Length <= 1) return 0;
         string newStr = currentStr.Substring(0, currentStr.Length - 1);
         return decimal.TryParse(newStr, out decimal result) ? result : 0;
     });

    public void IncrementQuantity()
    => UpdateQuantity(current => current + 1);

    public void DecrementQuantity()
    => UpdateQuantity(current => current - 1 <= 0 ? 1 : current - 1);

    public void UpdateQuantity(Func<decimal, decimal> updateFunc)
    {
        if (SelectedItem != null)
        {
            SelectedItem.Quantity = updateFunc(SelectedItem.Quantity);
            RecalculateItemTotals(SelectedItem);
            UpdateAmount();
            CalculateSection4Table();
            NotifyStateChanged();
        }
    }

    public void RecalculateItemTotals(TableItem item)
    {
        // Active quantity is the total quantity minus any voided amount
        decimal activeQty = item.Quantity - (item.VoidAmount ?? 0);
        if (activeQty < 0) activeQty = 0;

        decimal basePrice = item.Price ?? 0M;

        decimal originalTotal = activeQty * basePrice;
        item.Total = originalTotal; // gross before discount

        // Apply line-item discount to the active portion
        if (item.HasDiscount)
        {
            if (item.DiscountPercentage.HasValue && item.DiscountPercentage.Value > 0)
            {
                decimal discountAmount = originalTotal * (item.DiscountPercentage.Value / 100);
                item.TotalDiscountPrice = discountAmount;
                item.TotalAfterDiscount = originalTotal - discountAmount;
            }
            else if (item.DiscountAmount.HasValue && item.DiscountAmount.Value > 0)
            {
                // Note: DiscountAmount is usually per line. If partial void happens, 
                // we might need to decide if the amount should be prorated.
                // For now, if it's a fixed amount, we keep it as is unless it exceeds the total.
                item.TotalDiscountPrice = Math.Min(item.DiscountAmount.Value, originalTotal);
                item.TotalAfterDiscount = originalTotal - item.TotalDiscountPrice.Value;
            }
            else
            {
                item.TotalDiscountPrice = 0;
                item.TotalAfterDiscount = null;
            }
        }
        else
        {
            item.TotalDiscountPrice = 0;
            item.TotalAfterDiscount = null;
        }

        // Apply per-item tax on the after-discount amount of active quantity
        decimal subtotalForTax = item.TotalAfterDiscount ?? originalTotal;
        if (item.HasTax && item.ItemTax.HasValue && item.ItemTax.Value > 0)
        {
            item.ItemTaxAmount = Math.Round(subtotalForTax * (item.ItemTax.Value / 100), 2);
        }
        else
        {
            item.ItemTaxAmount = 0;
        }

        // TotalAmount = net after discount + item-level tax
        item.TotalAmount = subtotalForTax + item.ItemTaxAmount;
    }

    public void RemoveItem(List<TableItem> items)
    {
        if (SelectedItem != null && items.Contains(SelectedItem))
        {
            items.Remove(SelectedItem);
        }

        CalculateTotalAmount(items);
        CalculateSection4Table();
        NotifyStateChanged();
    }

    private void CalculateTotalAmount(List<TableItem> items)
     => UpdateAmount();


    private void UpdateAmount()
    {
        if (_commonProperties?.TableItems == null) return;

        // Force recalculation of each item to ensure active quantities are used
        foreach (var item in _commonProperties.TableItems)
        {
            RecalculateItemTotals(item);
        }

        // Current net total for active items only
        decimal netTotalBeforeTax = _commonProperties.TableItems
            .Sum(i => i.TotalAfterDiscount ?? i.Total ?? 0M);

        decimal grossTotal = _commonProperties.TableItems
            .Sum(i => i.Total ?? 0M);

        decimal totalLineDiscount = _commonProperties.TableItems
            .Sum(i => i.TotalDiscountPrice ?? 0M);

        _commonProperties.TotalLineDiscount = totalLineDiscount;

        // Account (Index 0): Gross total before any discount or tax
        if (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 0)
        {
            _commonProperties._financeSettingsList[0].Value = FormatValue(grossTotal);
        }

        // SubTotal = net price before any taxes (item or order level)
        // This is the base for calculating order-level Service/Tax
        _commonProperties.SubTotal = netTotalBeforeTax;
    }

    private decimal FormatValue(decimal value)
    {
        return value % 1 == 0 ? Math.Truncate(value) : Math.Round(value, 2);
    }

    public void CalculateTotalAmountFromTableItems(List<TableItem> items)
    {
        CalculateTotalAmount(items);
        NotifyStateChanged();
    }

    public void UpdateFinanceSettingsByMode(string posMode)
    {
        if (_commonProperties == null) return;

        var orderType = posMode switch
        {
            nameof(PosModes.TakeAway) => PosModes.TakeAway.ToString(),
            nameof(PosModes.Delivery) => PosModes.Delivery.ToString(),
            nameof(PosModes.DineIn) => PosModes.DineIn.ToString(),
            _ => null
        };

        if (orderType == null) return;

        _commonProperties._financeSettingsList = new()
        {
            new FinanceSettings { Label = "Account", Value = 0M },
            new FinanceSettings { Label = "Discount", Value = 0M },
            new FinanceSettings { Label = "Tax", Value = 0M },
            new FinanceSettings { Label = (posMode == nameof(PosModes.Delivery) ? "DeliveryService" : "Service"), Value = posMode == nameof(PosModes.Delivery) ? (_commonProperties?.CustomerDetails?.ZoneFees ?? 0M) : 0M },
            new FinanceSettings { Label = "Total", Value = 0M }
        };

        UpdateAmount();
        CalculateSection4Table();
        NotifyStateChanged();
    }


    public void CalculateTotalOrderPrice()
    {
        decimal netAmountAfterLine = _commonProperties!.SubTotal ?? 0M;

        // Apply discount to subtotal first before Tax/Service
        decimal amountAfterOrderDiscount = netAmountAfterLine;
        decimal orderDiscountValue = 0M;

        if (_commonProperties.IsHospitalityMode)
        {
            orderDiscountValue = netAmountAfterLine;
            if (_commonProperties.OrderDiscount != null) 
            {
                _commonProperties.OrderDiscount.Percentage = 100;
                _commonProperties.OrderDiscount.DiscountType = "Percentage";
            }
        }
        else if (_commonProperties.OrderDiscount != null)
        {
            if (_commonProperties.OrderDiscount.Percentage > 0)
            {
                orderDiscountValue = (netAmountAfterLine * _commonProperties.OrderDiscount.Percentage) / 100;
            }
            else if (_commonProperties.OrderDiscount.Value > 0)
            {
                orderDiscountValue = _commonProperties.OrderDiscount.Value;
            }
        }

        if (orderDiscountValue > netAmountAfterLine) orderDiscountValue = netAmountAfterLine;
        amountAfterOrderDiscount -= orderDiscountValue;

        // Update total discount
        decimal totalLineDiscount = _commonProperties.TotalLineDiscount ?? 0M;
        decimal totalCombinedDiscount = totalLineDiscount + orderDiscountValue;
        
        _commonProperties.TotalDiscount = totalCombinedDiscount;

        // Update Discount Reason as indicator
        ApplyDiscountAfterTotal(totalLineDiscount); // Keeps the reason string updating clean

        if (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 1)
        {
            _commonProperties._financeSettingsList[1].Value = FormatValue(totalCombinedDiscount);
        }

        switch (_commonProperties!.CurrentPosMode)
        {
            case "TakeAway":
                _commonProperties.TotalOrderPrice = CalculateAmountAfterOrderSettings(_commonProperties.TakeAwaySettings!, amountAfterOrderDiscount);
                break;
            case "DineIn":
                _commonProperties.TotalOrderPrice = CalculateAmountAfterOrderSettings(_commonProperties.DineInSettings!, amountAfterOrderDiscount);
                break;
            case "Delivery":
                _commonProperties.TotalOrderPrice = CalculateAmountAfterOrderSettings(_commonProperties.DeliverySettings!, amountAfterOrderDiscount);
                break;
        }

        // After settings are applied, TotalOrderPrice includes taxes/services on the discounted subtotal.
        _commonProperties.TotalAmountAfterDiscount = _commonProperties.TotalOrderPrice ?? 0M;
        
        if (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4)
        {
            decimal finalTotal = _commonProperties.TotalAmountAfterDiscount;
            _commonProperties._financeSettingsList[4].Value = FormatValue(finalTotal);
        }
    }

    public void ApplyDiscountAfterTotal(decimal totalLineDiscount = 0M)
    {
        string lineDiscountIndicator = "[خصم أصناف]";
        if (_commonProperties!.OrderDiscount != null)
        {
            if (totalLineDiscount > 0)
            {
                if (string.IsNullOrEmpty(_commonProperties.OrderDiscount.DiscountReason))
                {
                    _commonProperties.OrderDiscount.DiscountReason = lineDiscountIndicator;
                }
                else if (!_commonProperties.OrderDiscount.DiscountReason.Contains(lineDiscountIndicator))
                {
                    _commonProperties.OrderDiscount.DiscountReason = $"{lineDiscountIndicator} {_commonProperties.OrderDiscount.DiscountReason}";
                }
            }
            else if (!string.IsNullOrEmpty(_commonProperties.OrderDiscount.DiscountReason))
            {
                _commonProperties.OrderDiscount.DiscountReason = _commonProperties.OrderDiscount.DiscountReason.Replace(lineDiscountIndicator, "").Trim();
            }
        }
    }

    public void CalculateSection4Table()
    {
        UpdateAmount();
        CalculateTotalOrderPrice();
        NotifyStateChanged();
    }

    public decimal CalculateAmountAfterOrderSettings<T>(T orderSettings, decimal Amount)
    {
        if (orderSettings is TakeAwaySettings takeawaySettings)
        {
            return CalculateTotalAmountHelper(takeawaySettings, Amount);
        }

        if (orderSettings is DeliverySettings deliverySettings)
        {
            return CalculateTotalAmountHelper(deliverySettings, Amount);
        }

        if (orderSettings is DineInSettings dinInSettings)
        {
            return CalculateTotalAmountHelper(dinInSettings, Amount);
        }

        // Fallback for Delivery mode if settings are missing but a zone fee might exist
        if (_commonProperties?.CurrentPosMode == nameof(PosModes.Delivery))
        {
            return CalculateTotalAmountHelper(new DeliverySettings { Service = 0, Tax = 0 }, Amount);
        }

        return 0M;
    }

    private decimal CalculateTotalAmountHelper(dynamic settings, decimal Amount)
    {
        decimal orderTaxRate = settings.Tax ?? 0M;
        decimal serviceRate = settings.Service ?? 0M;

        // 1. Calculate order-level tax based on subtotal
        decimal orderTaxAmount = (orderTaxRate * Amount) / 100;
        
        // 2. Calculate service based on subtotal
        decimal serviceAmount = (serviceRate * Amount) / 100;

        if (_commonProperties?.CurrentPosMode == "Delivery" && _commonProperties?.CustomerDetails?.ZoneFees > 0)
        {
            serviceAmount = _commonProperties.CustomerDetails.ZoneFees;
        }

        // 3. Sum of all per-item taxes
        decimal totalItemTax = _commonProperties?.TableItems?.Sum(i => i.ItemTaxAmount) ?? 0M;

        // 4. Combined Tax for Display (Order Tax + Item Taxes)
        decimal combinedTaxForDisplay = orderTaxAmount + totalItemTax;

        // Store these in finance settings labels
        if (_commonProperties?._financeSettingsList != null)
        {
            if (_commonProperties._financeSettingsList.Count > 2)
                _commonProperties._financeSettingsList[2].Value = FormatValue(combinedTaxForDisplay);
            if (_commonProperties._financeSettingsList.Count > 3)
                _commonProperties._financeSettingsList[3].Value = FormatValue(serviceAmount);
        }

        // Total = Net Amount + Combined Taxes + Service
        var totalAmount = Amount + totalItemTax + orderTaxAmount + serviceAmount;
        
        return Math.Round(totalAmount * 2, MidpointRounding.AwayFromZero) / 2;
    }
    public async void NotifyStateChanged()
     => OnChange?.Invoke();

    public void ClearDineInOrderAttributes()
    {
        _commonProperties!.TotalDiscount = 0M;
        _commonProperties._financeSettingsList![0].Value = 0M;
        _commonProperties.TotalOrderPrice = 0M;
        _commonProperties.TableItems!.Clear();
        _commonProperties.VoidedTableItems!.Clear();
        _commonProperties.AppendedTableItems!.Clear();
        _commonProperties.UpdateDineInOrder = false;
        _commonProperties!.CurrentDineInOrder = new();
        _commonProperties.DineInOrderValues = new();
        _commonProperties!.OrderDiscount = new();
        _commonProperties.CustomerName = "";
        _commonProperties.CustomerPhone = "";
        _commonProperties.SelectedPaymentMethod = PaymentMethod.Cash;
        _commonProperties.ClearStaffMeal();
        _commonProperties.ClearHospitality();
    }

    public void ClearTakeAwayOrderAttributes()
    {
        _commonProperties!.TotalDiscount = 0M;
        _commonProperties._financeSettingsList![0].Value = 0M;
        _commonProperties.TotalOrderPrice = 0M;
        _commonProperties.TableItems!.Clear();
        _commonProperties.VoidedTableItems!.Clear();
        _commonProperties.AppendedTableItems?.Clear();
        _commonProperties!.OrderDto = new();
        _commonProperties.OrderNote = string.Empty;
        _commonProperties.CustomerName = string.Empty;
        _commonProperties.CustomerPhone = string.Empty;
        _commonProperties.CustomerDetails = new();
        _commonProperties.SelectedPaymentMethod = PaymentMethod.Cash;
        _commonProperties.UpdateDeliveryOrder = false;
        _commonProperties.UpdateDineInOrder = false;
        _commonProperties.OrderDiscount = new();
        _commonProperties.ClearStaffMeal();
        _commonProperties.ClearHospitality();
        
        NotifyStateChanged();
    }

    public void RemoveItemDiscount()
    {
        if (SelectedItem == null) return;
        SelectedItem.HasDiscount = false;
        SelectedItem.DiscountPercentage = null;
        SelectedItem.DiscountAmount = null;
        SelectedItem.DiscountReason = null;
        SelectedItem.TotalDiscountPrice = 0;
        RecalculateItemTotals(SelectedItem);
        UpdateAmount();
        CalculateSection4Table();
        NotifyStateChanged();
    }

    public string? AddItemComment(string comment)
    {
        if (SelectedItem is null )
            return null;

        var tableItem = _commonProperties!.TableItems!
            .FirstOrDefault(i => i.Id == SelectedItem.Id);

        if (tableItem == null) return null;

        var random = new Random();
        var commentAttribute = new AttributeDto()
        {
            Id = random.Next(5000, 100000),
            Name = comment
        };

        tableItem.Attributes ??= new List<AttributeDto>();
        tableItem.Attributes.Add(commentAttribute);

        UpdateTableItemLineComment(tableItem);

        NotifyStateChanged();

        return "success";
    }

    public string? EditItemComment(string oldComment, string newComment)
    {
        if (SelectedItem == null || SelectedItem.Id == 0)
            return null;

        var tableItem = _commonProperties!.TableItems!
            .FirstOrDefault(i => i.Id == SelectedItem.Id);

        if (tableItem == null) return null;

        var existingComment = tableItem.Attributes?
            .FirstOrDefault(attr => attr.Name == oldComment);

        if (existingComment == null)
            return "comment not found";

        existingComment.Name = newComment;

        UpdateTableItemLineComment(tableItem);

        NotifyStateChanged();

        return "success";
    }

    public string? DeleteItemComment(string comment)
    {
        if (SelectedItem == null || SelectedItem.Id == 0)
            return null;

        var tableItem = _commonProperties!.TableItems!
            .FirstOrDefault(i => i.Id == SelectedItem.Id);

        if (tableItem == null) return null;

        var commentToRemove = tableItem.Attributes?
            .FirstOrDefault(attr => attr.Name == comment);

        if (commentToRemove == null)
            return "comment not found";

        tableItem.Attributes?.Remove(commentToRemove);

        UpdateTableItemLineComment(tableItem);

        NotifyStateChanged();

        return "success";
    }

    private void UpdateTableItemLineComment(TableItem tableItem)
    {
        var comments = tableItem.Attributes?
            .Where(attr => attr.Id >= 5000 && !string.IsNullOrWhiteSpace(attr.Name))
            .Select(attr => attr.Name!.Trim())
            .ToList();

        tableItem.LineComment = (comments != null && comments.Any())
            ? string.Join(", ", comments)
            : null;
    }

    public void AddItemDiscount(string discountType, decimal discountValue, string? discountReason = null)
    {
        if (SelectedItem == null || SelectedItem.Id == 0)
            return;

        SelectedItem.HasDiscount = true;
        SelectedItem.DiscountReason = discountReason;
        if (discountType == "Percentage")
        {
            SelectedItem.DiscountPercentage = discountValue;
            SelectedItem.DiscountAmount = null;
        }
        else if (discountType == "Value")
        {
            SelectedItem.DiscountAmount = discountValue;
            SelectedItem.DiscountPercentage = null;
        }

        RecalculateItemTotals(SelectedItem);

        UpdateAmount();
        CalculateSection4Table();
        NotifyStateChanged();
    }

    public void ApplyOrderDiscount(decimal discountValue, bool isPercentage, DiscountReason reason)
    {
        if (_commonProperties!.OrderDiscount == null)
            _commonProperties.OrderDiscount = new();

        if (isPercentage)
        {
            _commonProperties.OrderDiscount.Percentage = discountValue;
            _commonProperties.OrderDiscount.Value = 0;
            _commonProperties.OrderDiscount.DiscountType = "Percentage";
        }
        else
        {
            _commonProperties.OrderDiscount.Value = discountValue;
            _commonProperties.OrderDiscount.Percentage = 0;
            _commonProperties.OrderDiscount.DiscountType = "Value";
        }

        _commonProperties.OrderDiscount.DiscountReason = reason.ToString();

        CalculateSection4Table();
        NotifyStateChanged();
    }

    public void ResetDiscount()
    {
        if (_commonProperties!.OrderDiscount != null)
        {
            _commonProperties.OrderDiscount.Percentage = 0;
            _commonProperties.OrderDiscount.Value = 0;
            _commonProperties.OrderDiscount.DiscountType = null;
            _commonProperties.OrderDiscount.DiscountReason = null;
        }

        CalculateSection4Table();
        NotifyStateChanged();
    }
}
