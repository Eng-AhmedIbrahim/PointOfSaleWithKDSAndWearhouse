using BlazorBase.ERPFrontServices.ComplaintServices;
using BlazorBase.ERPFrontServices.DeliveryServices;
using POS.Desktop.Components.PosDialog;
using POS.Contract.Dtos.DeliveryDtos.DeliveryCustomerInfo;

namespace POS.Desktop.Components.DeliveryComponent;

public partial class DeliveryButtons
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private IComplaintServices _complaintServices { get; set; } = default!;
    [Inject] private IPosFeatureSettingsService _featureSettingsService { get; set; } = default!;
    [Inject] private CartService _cartService { get; set; } = default!;

    private bool _canOrder;
    private bool _canAddNew;
    private bool _canClear;
    private bool _canComplaints;
    private bool _canSearch;
    private bool _canBranchMgmt;
    private bool _canDistribution;
    private bool _canToggleDir;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is { IsAuthenticated: true })
        {
            _canOrder        = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryOrderBtn")).Succeeded;
            _canAddNew       = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryAddNewBtn")).Succeeded;
            _canClear        = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryClearBtn")).Succeeded;
            _canComplaints   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryComplaintsBtn")).Succeeded;
            _canSearch       = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliverySearchBtn")).Succeeded;
            _canBranchMgmt   = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryBranchManagementBtn")).Succeeded;
            _canDistribution = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryDistributionBtn")).Succeeded;
            _canToggleDir    = (await AuthorizationService.AuthorizeAsync(user, "CanAccessDeliveryToggleDirectionBtn")).Succeeded;
        }
    }

    private async Task SearchCustomer()
    {
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await _dialogService.ShowAsync<CustomerSearchDialog>(
            Localizer["Search"], options);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data is DeliveryCustomerToReturnDto selected)
        {
            // Populate phone so that HandleKeyDown logic kicks in as if the user typed and pressed Enter
            _commonProperties.CustomerDetails ??= new();
            _commonProperties.CustomerDetails.FirstPhoneNumber = selected.FirstPhoneNumber;

            // Populate basic info directly
            _commonProperties.CustomerDetails.CustomerName = selected.CustomerName;
            _commonProperties.CustomerDetails.SecondPhoneNumber = selected.SecondPhoneNumber;
            _commonProperties.CustomerDetails.ClientTitle = selected.ClientTitle;
            _commonProperties.CustomerDetails.Id = selected.Id;

            // Populate first address if available
            var firstAddr = selected.CustomerAddresses?.FirstOrDefault();
            if (firstAddr != null)
            {
                _commonProperties.CustomerDetails.ClientAddress = firstAddr.ClientAddress;
                _commonProperties.CustomerDetails.FloorNumber = firstAddr.FloorNumber;
                _commonProperties.CustomerDetails.FlatNumber = firstAddr.FlatNumber;
                _commonProperties.CustomerDetails.HomeNumber = firstAddr.HomeNumber;
                _commonProperties.CustomerDetails.AddressNote = firstAddr.AddressNote;
                _commonProperties.CustomerDetails.ZoneID = firstAddr.DeliveryZoneId;
                _commonProperties.CustomerDetails.ZoneName = firstAddr.ZoneName;
                _commonProperties.CustomerDetails.ZoneFees = firstAddr.DeliveryFee;
                _commonProperties.CustomerDetails.ZoneBonus = firstAddr.ZoneBonus;
                _commonProperties.CustomerDetails.BranchId = firstAddr.BranchId;
                _commonProperties.CustomerDetails.BranchName = firstAddr.BranchName;
            }

            // Trigger address-selected callback so Delivery.razor syncs zone UI
            _handelDeliveryInvocation.TriggerAddressSelected();
            _snackbar.Add(
                (_isArabic ? $"تم تحميل بيانات العميل: {selected.CustomerName}" : $"Customer loaded: {selected.CustomerName}"),
                Severity.Success);
        }
    }

    private bool _isArabic => Localizer.GetCurrentLanguage() == "ar";

    private async Task ShowComplaints()
    {
        if (string.IsNullOrEmpty(_commonProperties?.CustomerDetails?.CustomerName))
        {
            _snackbar.Add(Localizer["SearchCustomerFirst"], Severity.Warning);
            return;
        }

        var parameters = new DialogParameters();
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };

        var dialog = await _dialogService.ShowAsync<ComplaintActionDialog>("Complaints", parameters, options);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data is string action)
        {
            if (action == "New")
                await CreateNewComplaint();
            else if (action == "View")
                await SafeNavigateAsync("/delivery-complaints");
        }
    }

    private async Task CreateNewComplaint()
    {
        if (string.IsNullOrEmpty(_commonProperties?.CustomerDetails?.CustomerName))
        {
            _snackbar.Add(Localizer["SearchCustomerFirst"], Severity.Warning);
            return;
        }

        var targetOrder = _commonProperties.CustomerDetails.ActiveOrder ?? 
        _commonProperties.CustomerDetails.Last10Orders?.FirstOrDefault();
        
        var complaintDto = new ComplaintDto
        {
            CustomerId = _commonProperties.CustomerDetails.Id,
            CustomerName = _commonProperties.CustomerDetails.CustomerName,
            CustomerPhone = _commonProperties.CustomerDetails.FirstPhoneNumber,
            OrderId = targetOrder?.OrderId,
            OrderDatabaseId = targetOrder?.Id
        };

        var parameters = new DialogParameters { ["ComplaintDto"] = complaintDto };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };

        var dialog = await _dialogService.ShowAsync<ComplaintDialog>(Localizer["NewComplaint"], parameters, options);
        var result = await dialog.Result;
        
        if (!result!.Canceled && result.Data is ComplaintDto savedComplaint)
        {
            // Optional: You could reload complaints if there's a local list being displayed
        }
    }

    private async Task AddCustomer()
    {
        if (string.IsNullOrWhiteSpace(_commonProperties.CustomerDetails?.CustomerName))
        {
            _snackbar.Add(Localizer["CustomerNameRequired"], Severity.Warning);
            return;
        }

        if ((_commonProperties.CustomerDetails?.ZoneID ?? 0) == 0 && string.IsNullOrWhiteSpace(_commonProperties.CustomerDetails?.ZoneName))
        {
             _snackbar.Add(Localizer["ZoneRequired"], Severity.Warning);
             return;
        }

        // Ensure Branch info is set if missing
        if (string.IsNullOrEmpty(_commonProperties.CustomerDetails!.BranchName))
        {
            if (_commonProperties.BranchDetails != null)
            {
                 _commonProperties.CustomerDetails.BranchName = _commonProperties.BranchDetails.Name;
                 _commonProperties.CustomerDetails.BranchId = _commonProperties.BranchDetails.Id;
            }
        }

        var client = await _deliveryServices.GetClientByPhoneNumberAsync(_commonProperties!.CustomerDetails!.FirstPhoneNumber!);
        if (client == null || client.Id == 0)
        {
            var customerDetails = BackupCustomer() ?? new();
            var createdCustomer = await _deliveryServices.CreateClientAsync(customerDetails);
            if (createdCustomer != null && createdCustomer.Id > 0)
            {
                _commonProperties.CustomerDetails.Id = createdCustomer.Id;
                await SetPosModeToDeliveryMode();
                return;
            }
            else
            {
                _snackbar.Add("Customer Not Created", Severity.Error);
                return;
            }
        }

        _commonProperties.CustomerDetails.Id = client.Id;
        await SetPosModeToDeliveryMode();
    }

    private async Task BackToPos()
    {
        _commonProperties.AppendedTableItems!.Clear();
        _commonProperties.CustomerDetails = new();
        _commonProperties.CurrentPosMode = "TakeAway";
        _cartService.UpdateFinanceSettingsByMode("TakeAway");
        
        await SafeNavigateAsync("/pos");
    }


    private async Task SetPosModeToDeliveryMode()
    {
        _commonProperties.CurrentPosMode = PosModes.Delivery.ToString();

        _handelDeliveryInvocation.DeliveryDetails = string.Empty; // Keep header clean as per user request
        await SafeNavigateAsync("/pos");
    }

    private DeliveryCustomerDto BackupCustomer()
        => new DeliveryCustomerDto()
        {
            FirstPhoneNumber = _commonProperties.CustomerDetails!.FirstPhoneNumber,
            SecondPhoneNumber = _commonProperties.CustomerDetails!.SecondPhoneNumber,
            ClientTitle = _commonProperties.CustomerDetails!.ClientTitle,
            CustomerName = _commonProperties.CustomerDetails!.CustomerName,
            BranchName = _commonProperties.CustomerDetails!.BranchName,
            ZoneName = _commonProperties.CustomerDetails!.ZoneName,
            HomeNumber = _commonProperties.CustomerDetails!.HomeNumber,
            FloorNumber = _commonProperties.CustomerDetails!.FloorNumber,
            FlatNumber = _commonProperties.CustomerDetails!.FlatNumber,
            ClientAddress = _commonProperties.CustomerDetails!.ClientAddress,
            AddressNote = _commonProperties.CustomerDetails!.AddressNote,
            DeliveryZoneId = _commonProperties.CustomerDetails!.ZoneID,
            BranchId = _commonProperties.CustomerDetails!.BranchId
        };

    private void ClearDeliveryOrder()
    {
        _commonProperties.CustomerDetails = new();
        _handelDeliveryInvocation.CustomerDetails = new();
        _handelDeliveryInvocation.TriggerClearDelivery();
    }


    private async Task AddNewAddress()
    {
        if (string.IsNullOrEmpty(_commonProperties.CustomerDetails!.FirstPhoneNumber))
        {
            _snackbar.Add("Please Enter Customer Phone Number", Severity.Error);
            return;
        }

        ClearAddressDateToAddNewData();
        _commonProperties.DialogReference = await _dialogService.ShowAsync<AddNewAddressDialog>("Add New Address");
    }

    private void ClearAddressDateToAddNewData()
    {
        _handelDeliveryInvocation.CustomerDetails = CustomerDetails.Clone(_commonProperties.CustomerDetails!);

        _commonProperties.CustomerDetails!.ClientAddress = string.Empty;
        _commonProperties.CustomerDetails.FloorNumber = string.Empty;
        _commonProperties.CustomerDetails.FlatNumber = string.Empty;
        _commonProperties.CustomerDetails.HomeNumber = string.Empty;
        _commonProperties.CustomerDetails.AddressNote = string.Empty;
        _commonProperties.CustomerDetails.BranchName = string.Empty;
        _commonProperties.CustomerDetails.ZoneName = string.Empty;
    }

    private async Task ToggleDirection()
    {
        _handelDeliveryInvocation.TriggerToggleDirection();
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
}
