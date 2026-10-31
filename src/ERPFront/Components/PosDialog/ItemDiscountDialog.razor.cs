namespace ERPFront.Components.PosDialog;

public partial class ItemDiscountDialog
{
    private string DiscountType = "Percentage";
    private string DiscountPercentage = "Percentage";
    private string DiscountValue = "Value";
    private decimal DiscountAmount = 0;

    private string GetAdornment() => DiscountType == "Percentage" ? "%" : "EGP";
    private bool IsApplyDisabled => DiscountAmount <= 0;

    private void ApplyDiscount()
    {
        _cartService.AddItemDiscount(DiscountType, DiscountAmount);
        CloseDialog();
    }

    private void CloseDialog() => _commonProperties.ItemDiscountDialogReference?.Close();
}
