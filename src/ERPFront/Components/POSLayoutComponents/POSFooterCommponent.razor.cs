namespace ERPFront.Components.POSLayoutComponents;

public partial class POSFooterCommponent
{
    private bool canAccessDiscount;
    private bool canAccessMeals;
    private bool canAccessWaiting;
    private bool canAccessSettings;

    private bool _drawerOpen;
    private bool _open = false;
    private Anchor _anchor;
    private string discountType = "percentage";
    private DiscountReason SelectedReason { get; set; } = DiscountReason.LoyaltyReward;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;


        if (user.Identity is not { IsAuthenticated: true })
        {
            canAccessDiscount = false;
            canAccessMeals = false;
            canAccessWaiting = false;
            canAccessSettings = false;
            return;
        }
        canAccessDiscount = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDiscount")).Succeeded;
        canAccessMeals = (await AuthorizationService.AuthorizeAsync(user, "CanAccessMeals")).Succeeded;
        canAccessWaiting = (await AuthorizationService.AuthorizeAsync(user, "CanAccessWaiting")).Succeeded;
        canAccessSettings = (await AuthorizationService.AuthorizeAsync(user, "CanAccessSettings")).Succeeded;
    }



    private void ApplyDiscount()
    {
        if (discountType == "percentage")
        {
            commonProperties.OrderDiscount = new OrderDiscount()
            {
                DiscountType = "percentage",
                Percentage = commonProperties.DiscountPercentage,
                Value = 0M,
                DiscountReason = SelectedReason.ToString()
            };
            commonProperties.TotalDiscount = commonProperties.OrderDiscount.Percentage;
        }
        else
        {
            commonProperties.OrderDiscount = new OrderDiscount()
            {
                DiscountType = "value",
                Percentage = 0M,
                Value = commonProperties.DiscountValue,
                DiscountReason = SelectedReason.ToString()
            };
            commonProperties.TotalDiscount = commonProperties.OrderDiscount.Value;
        }

        _open = false;

        _cartService.CalculateSection4Table();
    }

    private void GotoWaitingPage()
    {
        if (commonProperties!.TableItems!.Count() > 0)
        {
            Snackbar.Add("Please Finish The Order First", Severity.Warning);
        }
        else
        {
            navigationManager.NavigateTo("/waitingPage");
        }
    }

  

    private void OpenDrawer(Anchor anchor)
    {
        _anchor = anchor;
        _open = true;
    }

    private void ToggleDrawer()
        => _drawerOpen = !_drawerOpen;

    private void CloseDrawer()
      => _open = false;

}