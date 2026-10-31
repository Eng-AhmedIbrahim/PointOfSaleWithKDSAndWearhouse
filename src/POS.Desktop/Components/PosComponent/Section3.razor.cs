using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using BlazorBase.Services;
using MudBlazor;

namespace POS.Desktop.Components.PosComponent;

public partial class Section3
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private bool _canUseKeypad;
    private bool _canCustomerData;
    private bool _canIncrement;
    private bool _canDecrement;
    private bool _canDeleteItem;
    private bool _canEditItem;
    private bool _canApplyDiscount;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is { IsAuthenticated: true })
        {
            _canUseKeypad     = (await AuthorizationService.AuthorizeAsync(user, "CanUseKeypad")).Succeeded;
            _canCustomerData  = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterCustomerDataBtn")).Succeeded;
            _canIncrement     = (await AuthorizationService.AuthorizeAsync(user, "CanIncrementQuantity")).Succeeded;
            _canDecrement     = (await AuthorizationService.AuthorizeAsync(user, "CanDecrementQuantity")).Succeeded;
            _canDeleteItem    = (await AuthorizationService.AuthorizeAsync(user, "CanDeleteItem")).Succeeded;
            _canEditItem      = (await AuthorizationService.AuthorizeAsync(user, "CanEditItemComment")).Succeeded;
            _canApplyDiscount = (await AuthorizationService.AuthorizeAsync(user, "CanApplyItemDiscount")).Succeeded;
        }
    }
}
