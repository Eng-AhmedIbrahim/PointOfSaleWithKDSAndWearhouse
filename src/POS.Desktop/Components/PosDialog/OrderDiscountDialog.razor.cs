using Microsoft.AspNetCore.Components;
using MudBlazor;
using BlazorBase.Models;
using POS.Contract.Models;
using BlazorBase.ERPFrontServices.CartServices;
using BlazorBase.Services;

namespace POS.Desktop.Components.PosDialog;

public partial class OrderDiscountDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;

    private string DiscountType { get; set; } = "Percentage";
    private decimal DiscountPercentage { get; set; } = 0;
    private decimal DiscountValue { get; set; } = 0;
    private string DiscountReasonText { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        if (_commonProperties.OrderDiscount != null && !string.IsNullOrEmpty(_commonProperties.OrderDiscount.DiscountType))
        {
            DiscountType = _commonProperties.OrderDiscount.DiscountType.Equals("value", StringComparison.OrdinalIgnoreCase) ? "Value" : "Percentage";
            DiscountPercentage = _commonProperties.OrderDiscount.Percentage;
            DiscountValue = _commonProperties.OrderDiscount.Value;
            DiscountReasonText = _commonProperties.OrderDiscount.DiscountReason ?? string.Empty;
        }
    }

    private void OnModeChanged(string mode)
    {
        DiscountType = mode;
        StateHasChanged();
    }

    private void ApplyDiscount()
    {
        if (string.IsNullOrWhiteSpace(DiscountReasonText))
        {
            _snackbar.Add(Localizer["DiscountReasonRequired"], Severity.Error);
            return;
        }

        _commonProperties.OrderDiscount = new OrderDiscount()
        {
            DiscountType = DiscountType,
            Percentage = DiscountType == "Percentage" ? DiscountPercentage : 0M,
            Value = DiscountType == "Value" ? DiscountValue : 0M,
            DiscountReason = DiscountReasonText
        };

        _cartService.CalculateSection4Table();
        _commonProperties.OrderDiscountDialogReference?.Close(DialogResult.Ok(true));
    }

    private void RemoveDiscount()
    {
        _cartService.ResetDiscount();
        _commonProperties.OrderDiscountDialogReference?.Close(DialogResult.Ok(true));
    }
}
