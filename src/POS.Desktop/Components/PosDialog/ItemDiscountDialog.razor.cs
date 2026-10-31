namespace POS.Desktop.Components.PosDialog;

public partial class ItemDiscountDialog
{
    public string DiscountType { get; set; } = "Percentage";
    private string DiscountPercentage = "Percentage";
    private string DiscountValue = "Value";
    public decimal DiscountAmount { get; set; } = 0;
    public string DiscountReasonText { get; set; } = string.Empty;
    
    protected override void OnInitialized()
    {
        if (_cartService.SelectedItem != null && _cartService.SelectedItem.HasDiscount)
        {
            DiscountReasonText = _cartService.SelectedItem.DiscountReason ?? string.Empty;
            if (_cartService.SelectedItem.DiscountPercentage.HasValue)
            {
                DiscountType = "Percentage";
                DiscountAmount = _cartService.SelectedItem.DiscountPercentage.Value;
            }
            else if (_cartService.SelectedItem.DiscountAmount.HasValue)
            {
                DiscountType = "Value";
                DiscountAmount = _cartService.SelectedItem.DiscountAmount.Value;
            }
        }
    }

    private string GetAdornment() => DiscountType == "Percentage" ? "%" : "EGP";
    private bool IsApplyDisabled => DiscountAmount <= 0;

    private void OnModeChanged(string mode)
    {
        DiscountType = mode;
        StateHasChanged();
    }

    private void ApplyDiscount()
    {
        _cartService.AddItemDiscount(DiscountType, DiscountAmount, DiscountReasonText);
        CloseDialog();
    }

    private void RemoveDiscount()
    {
        _cartService.RemoveItemDiscount();
        CloseDialog();
    }

    private void CloseDialog() => _commonProperties.ItemDiscountDialogReference?.Close();
}

