using POS.Desktop.Components.PosDialog;
using System.Windows;

namespace POS.Desktop.Components.POSLayoutComponents;

public partial class POSNavbarCommponent : IDisposable
{
    private bool canAccessTables;
    private bool canAccessDelivery;
    private bool canAccessTakeAway;
    private bool canAccessAccounts;
    private bool canAccessSummary;
    private bool canAccessDistribution;
    private bool canAccessOrders;
    
    // Store event handler reference for proper cleanup
    private Action? _stateChangedHandler;

    protected override async Task OnInitializedAsync()
    {
        // Create and store event handler reference
        _stateChangedHandler = async () =>
        {
            try
            {
                await InvokeAsync(StateHasChanged);
            }
            catch (ObjectDisposedException)
            {
                // Component already disposed, ignore
            }
            catch (Exception)
            {
                // Silently ignore other exceptions during state update
            }
        };
        
        _section4ButtonsServices.OnChanged += _stateChangedHandler;
        _commonProperties.OnChange += _stateChangedHandler;
        Localizer.OnLanguageChanged += _stateChangedHandler;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            canAccessTables = false;
            canAccessDelivery = false;
            canAccessTakeAway = false;
            canAccessAccounts = false;
            canAccessSummary = false;
            canAccessDistribution = false;
            canAccessOrders = false;
            return;
        }

        canAccessTables = (await AuthorizationService.AuthorizeAsync(user, "CanAccessTables")).Succeeded;
        canAccessDelivery = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDelivery")).Succeeded;
        canAccessTakeAway = (await AuthorizationService.AuthorizeAsync(user, "CanAccessTakeAway")).Succeeded;
        canAccessAccounts = (await AuthorizationService.AuthorizeAsync(user, "CanAccessAccounts")).Succeeded;
        canAccessSummary = (await AuthorizationService.AuthorizeAsync(user, "CanAccessSummary")).Succeeded;
        canAccessDistribution = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDistribution")).Succeeded;
        canAccessOrders = (await AuthorizationService.AuthorizeAsync(user, "CanAccessOrders")).Succeeded;
    }

    private async Task SetMode(string mode)
    {
        if (_commonProperties!.TableItems!.Any())
        {
            if (_commonProperties.CurrentPosMode == "DineIn" && 
                _commonProperties.UpdateDineInOrder == true && 
                (_commonProperties.AppendedTableItems == null || !_commonProperties.AppendedTableItems.Any()) &&
                _commonProperties.OrderDiscount == null)
            {
                 _cartService.ClearDineInOrderAttributes();
            }
            else 
            {
                _snackbar.Add(Localizer["PleaseCompleteOrder"], Severity.Warning);
                return;
            }
        }

        // Update state BEFORE navigation to prevent TargetInvocationException
        switch (mode)
        {
            case "TakeAway":
                {
                    _commonProperties.CurrentPosMode = "TakeAway";
                    _commonProperties.TableItems!.Clear();
                    _commonProperties.AppendedTableItems!.Clear();
                    _commonProperties.CurrentDineInOrder = null;
                    _commonProperties.DineInOrderValues = new();
                    _commonProperties.UpdateDineInOrder = false;
                    _commonProperties.OrderDiscount = new();
                    _commonProperties.CustomerName = string.Empty;
                    _commonProperties.CustomerPhone = string.Empty;
                    _commonProperties.CustomerDetails = new();
                    _commonProperties.TableItemDisabled = false;
                    break;
                }
            case "Delivery":
                {
                    _commonProperties.CurrentPosMode = "Delivery";
                    break;
                }
            case "Distribution":
                {
                    _commonProperties.CurrentPosMode = "Distribution";
                    break;
                }
            case "DineIn":
                {
                    _commonProperties.CurrentPosMode = "DineIn";
                    break;
                }
            case "Accounts":
                {
                    _commonProperties.CurrentPosMode = "Accounts";
                    break;
                }
            case "Summary":
                {
                    _commonProperties.CurrentPosMode = "Summary";
                    break;
                }
            case "AllOrders":
                {
                    _commonProperties.CurrentPosMode = "AllOrders";
                    break;
                }
        }
        
        _cartService.UpdateFinanceSettingsByMode(_commonProperties.CurrentPosMode);

        string targetUrl = mode switch
        {
            "TakeAway" => "/pos",
            "Delivery" => "/delivery",
            "Distribution" => "/distribution",
            "DineIn" => "/dinein",
            "Accounts" => "/accounts",
            "Summary" => "/summary",
            "AllOrders" => "/all-orders",
            _ => "/pos"
        };

        try 
        {
            // Navigate last - component will unmount after this
            await SafeNavigateAsync(targetUrl);
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Navigation Error: {ex.Message}", Severity.Error);
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
                if (_navigationManager != null)
                {
                    try
                    {
                        // Try to access Uri - if it throws, NavigationManager isn't ready yet
                        var currentUri = _navigationManager.Uri;
                        if (!string.IsNullOrEmpty(currentUri))
                        {
                            // Use InvokeAsync to ensure we're on the correct synchronization context
                            await InvokeAsync(() => _navigationManager.NavigateTo(uri, forceLoad: false));
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
                    _snackbar.Add($"Navigation failed: {ex.Message}", Severity.Error);
                    return;
                }
                
                // Exponential backoff
                delayMs *= 2;
            }
        }
    }

    private void OpenDebugTerminal()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        DialogService.Show<LogViewerDialog>("Debug Terminal", options);
    }

    private void ExitApp()
    {
        if (_commonProperties!.TableItems!.Any())
        {
            if (_commonProperties.CurrentPosMode == "DineIn" && 
                _commonProperties.UpdateDineInOrder == true && 
                (_commonProperties.AppendedTableItems == null || !_commonProperties.AppendedTableItems.Any()) &&
                _commonProperties.OrderDiscount == null)
            {
                 // Allowed to exit
            }
            else 
            {
                _snackbar.Add(Localizer["PleaseCompleteOrder"], Severity.Warning);
                return;
            }
        }
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        // Unsubscribe from all events to prevent memory leaks and exceptions
        if (_stateChangedHandler != null)
        {
            _section4ButtonsServices.OnChanged -= _stateChangedHandler;
            _commonProperties.OnChange -= _stateChangedHandler;
            Localizer.OnLanguageChanged -= _stateChangedHandler;
        }
    }
}
