using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using MudBlazor;
using POS.Contract.Dtos.OrderDtos;
using BlazorBase.Services;

namespace POS.Desktop.Components.DeliveryComponent;

public partial class ActiveOrderDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;
    [Inject] LocalizationService Localizer { get; set; } = default!;
    [Inject] IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter] public List<OrderDto> ActiveOrders { get; set; } = new();

    private bool _canVoidOrder = false;
    private bool _canEditOrder = false;
    private bool _canCreateOrder = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            _canVoidOrder   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryVoidBtn")).Succeeded;
            _canEditOrder   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryEditBtn")).Succeeded;
            _canCreateOrder = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryOrderBtn")).Succeeded;
        }
        catch
        {
            _canVoidOrder = false;
            _canEditOrder = false;
            _canCreateOrder = false;
        }
    }

    void CreateNew() => MudDialog.Close(DialogResult.Ok(false));
    void EditExisting(OrderDto order) => MudDialog.Close(DialogResult.Ok(order));

    async Task VoidExisting(OrderDto order)
    {
        var parameters = new DialogParameters
        {
            ["OrderId"]                  = order.Id,
            ["IsDineIn"]                 = false,
            ["IsDistribution"]           = true,
            ["InitialDistributionOrder"] = order
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = false };
        var dialog = await DialogService.ShowAsync<BlazorBase.Components.Shared.VoidOrderDialog>(Localizer["VoidOrder"], parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            MudDialog.Close(DialogResult.Ok("Voided"));
        }
    }
}
