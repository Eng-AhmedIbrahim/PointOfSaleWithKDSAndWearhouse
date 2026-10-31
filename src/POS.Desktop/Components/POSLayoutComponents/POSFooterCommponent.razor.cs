using POS.Core.Services.Contract;
using POS.Contract.Dtos.AccountDtos;
using POS.Desktop.Components.PosDialog;
using POS.Contract.Models;
using Serilog;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using BlazorBase.Services;
using BlazorBase.ERPFrontServices.CartServices;
using BlazorBase.ERPFrontServices.Section4ButtonsService;
using BlazorBase.ERPFrontServices.AppDateServices;
using BlazorBase.ERPFrontServices.CategoryServices;
using BlazorBase.Helpers;

namespace POS.Desktop.Components.POSLayoutComponents;

public partial class POSFooterCommponent : IDisposable
{
    private bool _canFooterDiscount;
    private bool _canFooterCustomerData;
    private bool _canFooterPaymentMethod;
    private bool _canFooterQuickPayment;
    private bool _canFooterMeals;
    private bool _canFooterWaiting;
    private bool _canFooterSettings;
    private bool _canPosSettingsFeature;
    private bool _canFooterHospitality;

    [Inject] public required LocalizationService LocalizationService { get; set; }
    [Inject] public IStaffMealService? _staffMealService { get; set; }
    [Inject] public required IPrintOrderService _printOrderService { get; set; }
    [Inject] public required ICategoryServices _categoryServices { get; set; }
    private bool _drawerOpen;
    private Action? _stateChangedHandler;

    protected override async Task OnInitializedAsync()
    {
        _stateChangedHandler = async () => 
        {
            try { await InvokeAsync(StateHasChanged); } catch { }
        };

        _commonProperties.OnChange += _stateChangedHandler;
        LocalizationService.OnLanguageChanged += _stateChangedHandler;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return;
        }

        _canFooterDiscount       = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterDiscountBtn")).Succeeded;
        _canFooterCustomerData   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterCustomerDataBtn")).Succeeded;
        _canFooterPaymentMethod  = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterPaymentMethodBtn")).Succeeded;
        _canFooterQuickPayment   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterQuickPaymentBtn")).Succeeded;
        _canFooterMeals          = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterMealsBtn")).Succeeded;
        _canFooterWaiting        = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterWaitingBtn")).Succeeded;
        _canFooterSettings       = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterSettingsBtn")).Succeeded;
        _canPosSettingsFeature   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessPosSettingsFeature")).Succeeded;
        _canFooterHospitality    = (await AuthorizationService.AuthorizeAsync(user, "CanAccessFooterHospitalityBtn")).Succeeded;
    }

    private async Task OpenOrderDiscountDialog()
    {
        _drawerOpen = false; 
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        _commonProperties.OrderDiscountDialogReference = await DialogService.ShowAsync<OrderDiscountDialog>(LocalizationService["OrderDiscount"], options);
    }

    private async Task GotoWaitingPage()
    {
        _drawerOpen = false;
        if (_commonProperties!.TableItems!.Any())
        {
            Snackbar.Add(LocalizationService["FinishOrderFirst"], Severity.Warning);
        }
        else
        {
            await SafeNavigateAsync("/waitingPage");
        }
    }

    private async Task SafeNavigateAsync(string uri)
    {
        int maxRetries = 5;
        int currentRetry = 0;
        int delayMs = 5;

        while (currentRetry < maxRetries)
        {
            try
            {
                await Task.Delay(delayMs);
                
                // Check if NavigationManager is initialized by safely checking the Uri property
                if (navigationManager != null)
                {
                    try
                    {
                        // Try to access Uri - if it throws, NavigationManager isn't ready yet
                        var currentUri = navigationManager.Uri;
                        if (!string.IsNullOrEmpty(currentUri))
                        {
                            // Use InvokeAsync to ensure we're on the correct synchronization context
                            await InvokeAsync(() => navigationManager.NavigateTo(uri, forceLoad: false));
                            return;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // NavigationManager not yet initialized, will retry
                        throw new InvalidOperationException("NavigationManager not yet initialized");
                    }
                }
                else
                {
                    throw new InvalidOperationException("NavigationManager is null");
                }
            }
            catch (Exception ex)
            {
                currentRetry++;
                
                if (currentRetry >= maxRetries)
                {
                    Snackbar.Add($"Navigation failed: {ex.Message}", Severity.Error);
                    return;
                }
                
                // Exponential backoff
                delayMs *= 2;
            }
        }
    }


    private async Task OpenCustomerInfoDialog()
    {
        _drawerOpen = false;
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        _commonProperties.CustomerInfoDialogReference = await DialogService.ShowAsync<CustomerInfoDialog>(LocalizationService["CustomerData"], options);
    }

    private async Task OpenPaymentMethodDialog()
    {
        _drawerOpen = false;
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        _commonProperties.PaymentMethodDialogReference = await DialogService.ShowAsync<PaymentModeDialog>(LocalizationService["PaymentMethod"], options);
    }

    private async Task OpenQuickPaymentDialog()
    {
        _drawerOpen = false;
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
        _commonProperties.QuickPaymentDialogReference = await DialogService.ShowAsync<QuickPaymentDialog>(LocalizationService["QuickPayment"], options);
    }

    private void ToggleDrawer()
        => _drawerOpen = !_drawerOpen;

    private async Task OpenMealsDialog()
    {
        _drawerOpen = false;
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            Snackbar.Add(LocalizationService["UserNotAuthenticated"], Severity.Error);
            return;
        }

        if (_staffMealService == null) return;

        var status = await _staffMealService.GetStatusByUserIdAsync(userId);
        
        var parameters = new DialogParameters { ["Status"] = status };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        
        var dialog = await DialogService.ShowAsync<StaffMealsDialog>(LocalizationService["StaffMeals"], parameters, options);
        var result = await dialog.Result;
        
        if (!result!.Canceled && result.Data != null)
        {
            try 
            {
                var data = (dynamic)result.Data;
                
                // 1. Clear current table items (it's a new staff order)
                _commonProperties.TableItems?.Clear();
                _commonProperties.OrderDiscount = new();
                _commonProperties.TotalDiscount = 0;
                _commonProperties.OrderNote = "";
                
                // 2. Activate Staff Mode
                _commonProperties.CurrentStaffMeal = data.Item1;
                _commonProperties.AllowedStaffItemIds = data.Item2;
                _commonProperties.AllowedStaffCategoryIds = data.Item3;
                _commonProperties.RemainingStaffMeals = data.Item4;
                _commonProperties.RemainingStaffMealAmount = data.Item5;
                _commonProperties.AllowedStaffMenuItems = data.Item6;
                
                Snackbar.Add(LocalizationService["StaffMealModeActivated"], Severity.Success);
                _commonProperties.NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error activating staff meal mode");
            }
        }
    }

    private async Task OpenHospitalityDialog()
    {
        _drawerOpen = false;
        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<HospitalityDialog>(LocalizationService["Hospitality"], options);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data != null)
        {
            try
            {
                if (result.Data != null)
                {
                    dynamic dialogData = result.Data;
                    UserDto? selectedUser = dialogData.Item1 as UserDto;
                    string? reason = dialogData.Item2 as string;

                // 1. Clear current table and state
                _commonProperties.TableItems?.Clear();
                _commonProperties.ClearStaffMeal();
                _commonProperties.OrderDiscount = new();
                _commonProperties.TotalDiscount = 0;
                _commonProperties.OrderNote = "";

                // 2. Activate Hospitality Mode
                _commonProperties.IsHospitalityMode = true;
                _commonProperties.HospitalityResponsiblePerson = selectedUser.ArabicName ?? selectedUser.UserName;
                _commonProperties.HospitalityReason = reason;

                // 3. Apply 100% Discount logic
                _commonProperties.OrderDiscount = new OrderDiscount
                {
                    DiscountType = "Percentage",
                    Percentage = 100,
                    Value = 0,
                    DiscountReason = $"{LocalizationService["Hospitality"]} - {reason}"
                };

                // 4. Reset mode to TakeAway for general flow
                _commonProperties.CurrentPosMode = "TakeAway";
                _cartService.UpdateFinanceSettingsByMode("TakeAway");
                _cartService.CalculateSection4Table();

                Snackbar.Add(LocalizationService["HospitalityModeActivated"], Severity.Success);
                _commonProperties.NotifyStateChanged();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error activating hospitality mode");
                Snackbar.Add(LocalizationService["ErrorActivatingHospitality"], Severity.Error);
            }
        }
    }

    public void Dispose()
    {
        if (_stateChangedHandler != null)
        {
            _commonProperties.OnChange -= _stateChangedHandler;
            LocalizationService.OnLanguageChanged -= _stateChangedHandler;
        }
    }
}