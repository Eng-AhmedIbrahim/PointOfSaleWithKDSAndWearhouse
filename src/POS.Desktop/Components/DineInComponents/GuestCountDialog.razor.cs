using Microsoft.AspNetCore.Components;
using MudBlazor;
using POS.Contract.Dtos.DineIn;
using BlazorBase.ERPFrontServices.DineInOrderServices;
using System.Threading.Tasks;

namespace POS.Desktop.Components.DineInComponents;

public partial class GuestCountDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int OrderId { get; set; }

    private DineInOrderDto Order { get; set; }
    private int MenCount { get; set; }
    private int WomenCount { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (OrderId > 0)
        {
            Order = await DineInOrderFrontService.GetDineInOrderByIdAsync(OrderId);
            
            if (Order != null)
            {
                MenCount = Order.MaleCount ?? 0;
                WomenCount = Order.FemaleCount ?? 0;
            }
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private void ChangeCount(string target, int change)
    {
        if (target == nameof(MenCount))
        {
            MenCount = Math.Max(0, MenCount + change);
        }
        else if (target == nameof(WomenCount))
        {
            WomenCount = Math.Max(0, WomenCount + change);
        }
    }

    private async Task Save()
    {
        if (Order == null) return;

        Order.MaleCount = MenCount;
        Order.FemaleCount = WomenCount;
        Order.CustomerCount = MenCount + WomenCount;

        // Ensure we pass a complete DTO, or at least one that satisfies update constraints
        // Using existing UpdateDineInOrderAsync which takes the DTO and updates specific fields
        
        var result = await DineInOrderFrontService.UpdateDineInOrderAsync(Order);
        if (result != null)
        {
            Snackbar.Add("Guest count updated successfully.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add("Failed to update guest count.", Severity.Error);
        }
    }
}
