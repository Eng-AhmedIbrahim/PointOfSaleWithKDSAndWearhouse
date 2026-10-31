using POS.Core.Services.Contract.PosFeatureServices;
using POS.Desktop.Services;

namespace POS.Desktop.Components.Pages.DeliveryPages;

public partial class Delivery : IDisposable
{
    [Inject] private ILocalStorageService _localStorage { get; set; } = default!;
    [Inject] private CallCenterNotificationService _callCenterService { get; set; } = default!;
    [Inject] private IPosFeatureSettingsService _featureSettingsService { get; set; } = default!;
    
    public IReadOnlyList<DeliveryTitleToReturnDto>? CustomerTitles { get; set; } = new List<DeliveryTitleToReturnDto>();
    public IReadOnlyList<BranchToReturnDto>? Branches { get; set; } = new List<BranchToReturnDto>();
    public DeliveryCustomerToReturnDto? CustomerStats { get; set; }

    private IReadOnlyList<DeliveryZonesToReturnDto> Zones = [];
    private string _pageDirection = "rtl";
    private bool _callCenterInitialized = false;
    private Action? _stateChangedHandler;
    
    protected override void OnInitialized()
    {
        _stateChangedHandler = async () => 
        {
            try { await InvokeAsync(StateHasChanged); } catch { }
        };

        Localizer.OnLanguageChanged += _stateChangedHandler;
        _handelDeliveryInvocation.OnAddressSelected += SafeAddressSelected;
        _handelDeliveryInvocation.OnClearDelivery += SafeClearDelivery;
        _handelDeliveryInvocation.OnToggleDirection += SafeToggleDirection;
        _handelDeliveryInvocation.OnNewOrderReceived += SafeNewOrderReceived;
        
        if (_commonProperties.CustomerDetails == null)
        {
            _commonProperties.CustomerDetails = new();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await LoadDirection();
                
                // Load initial data
                await GetAllTitles();
                await GetAllBranches();
                await GetZones();
                await LoadCustomerOrders();

                if (!_callCenterInitialized)
                {
                    await _callCenterService.InitializeAsync();
                    _callCenterInitialized = true;
                }

                if (_commonProperties.FeatureSettings == null || !_commonProperties.FeatureSettings.Any())
                {
                    _commonProperties.FeatureSettings = await _featureSettingsService.GetSettingsByComputerNameAsync(Environment.MachineName);
                }
                
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error loading Delivery Page: {ex.Message}", Severity.Error);
                Console.WriteLine($"Error in Delivery.OnAfterRenderAsync: {ex}");
            }
        }
    }

    private async Task LoadDirection()
    {
        var savedDirection = await _localStorage.GetItemAsync<string>("deliveryPageDirection");
        _pageDirection = string.IsNullOrEmpty(savedDirection) ? "rtl" : savedDirection;
    }

    private async Task LoadCustomerOrders()
    {
        // Example: _orders = await _deliveryServices.GetRecentCustomerOrdersAsync(_commonProperties.CustomerDetails.FirstPhoneNumber);
    }

    private async Task GetZones()
    {
        Zones = await _deliveryServices.GetAllDeliveryZonesAsync();
    }

    private async Task GetAllBranches()
    {
        Branches = await _branchService.GetBranches();
        if (_commonProperties.BranchDetails == null && Branches != null && Branches.Any())
        {
            _commonProperties.BranchDetails = Branches.FirstOrDefault();
        }
    }

    private async Task GetAllTitles()
       => CustomerTitles = await _deliveryServices.GetAllDeliveryCustomerTitlesAsync();

    private DeliveryZonesToReturnDto? SelectedZone;

    private void OnZoneChanged(DeliveryZonesToReturnDto zone)
    {
        SelectedZone = zone;
        if (zone != null)
        {
            _commonProperties.CustomerDetails!.ZoneName = zone.ZoneName;
            _commonProperties.CustomerDetails.ZoneID = zone.Id;
            _commonProperties.CustomerDetails.ZoneFees = zone.DeliveryFee ?? 0;
            _commonProperties.CustomerDetails.ZoneBonus = zone.ZoneBonus ?? 0;
            
            // Link the branch associated with this zone
            _commonProperties.CustomerDetails.BranchId = zone.BranchId;
            var branch = Branches?.FirstOrDefault(b => b.Id == zone.BranchId);
            if (branch != null)
            {
                _commonProperties.CustomerDetails.BranchName = branch.Name;
            }
        }
        else
        {
            _commonProperties.CustomerDetails!.ZoneName = string.Empty;
            _commonProperties.CustomerDetails.ZoneID = 0;
            _commonProperties.CustomerDetails.ZoneFees = 0;
            _commonProperties.CustomerDetails.ZoneBonus = 0;
            _commonProperties.CustomerDetails.BranchId = 0;
            _commonProperties.CustomerDetails.BranchName = string.Empty;
        }
        StateHasChanged();
    }

    private async void SafeAddressSelected()
    {
        try
        {
            if (_commonProperties != null && _commonProperties.CustomerDetails != null)
            {
                var targetZoneId = _commonProperties.CustomerDetails.ZoneID;
                var targetZoneName = _commonProperties.CustomerDetails.ZoneName;

                // Sync branch if needed
                if (_commonProperties.CustomerDetails.BranchId > 0 && 
                    (_commonProperties.BranchDetails == null || _commonProperties.BranchDetails.Id != _commonProperties.CustomerDetails.BranchId))
                {
                    _commonProperties.BranchDetails = Branches?.FirstOrDefault(b => b.Id == _commonProperties.CustomerDetails.BranchId) 
                        ?? new BranchToReturnDto { Id = _commonProperties.CustomerDetails.BranchId, Name = _commonProperties.CustomerDetails.BranchName };
                    
                    await GetZones();
                }

                if (targetZoneId > 0)   
                {
                    if (Zones != null && Zones.Any())
                    {
                        var zoneFromList = Zones.FirstOrDefault(z => z.Id == targetZoneId);
                        if (zoneFromList != null)
                        {
                            SelectedZone = zoneFromList;
                            _commonProperties.CustomerDetails.ZoneFees = zoneFromList.DeliveryFee ?? 0;
                            _commonProperties.CustomerDetails.ZoneBonus = zoneFromList.ZoneBonus ?? 0;
                        }
                        else
                        {
                            // Create a temporary zone object if not found in list to at least show the name
                            SelectedZone = new DeliveryZonesToReturnDto 
                            { 
                                Id = targetZoneId, 
                                ZoneName = targetZoneName,
                                DeliveryFee = _commonProperties.CustomerDetails.ZoneFees,
                                BranchId = _commonProperties.CustomerDetails.BranchId
                            };
                        }
                    }
                    else if (!string.IsNullOrEmpty(targetZoneName))
                    {
                         SelectedZone = new DeliveryZonesToReturnDto 
                         { 
                             Id = targetZoneId, 
                             ZoneName = targetZoneName,
                             DeliveryFee = _commonProperties.CustomerDetails.ZoneFees,
                             BranchId = _commonProperties.CustomerDetails.BranchId
                         };
                    }
                }
                else if (string.IsNullOrEmpty(targetZoneName))
                {
                    SelectedZone = null;
                }
            }
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SafeAddressSelected: {ex.Message}");
        }
    }

    private async void SafeClearDelivery()
    {
        try
        {
            _commonProperties.CustomerDetails = new();
            _handelDeliveryInvocation.CustomerDetails = new();
            SelectedZone = null;
            CustomerStats = null;
            _orders = new();
            _customerComplaints = new();
            _showComplaints = false;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SafeClearDelivery: {ex.Message}");
        }
    }

    private async void SafeToggleDirection()
    {
        try
        {
            _pageDirection = _pageDirection == "rtl" ? "ltr" : "rtl";
            await _localStorage.SetItemAsync("deliveryPageDirection", _pageDirection);
            await InvokeAsync(StateHasChanged);
        }
        catch { }
    }

    private async void SafeNewOrderReceived()
    {
        try
        {
            await LoadCustomerOrders();
            await InvokeAsync(StateHasChanged);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_stateChangedHandler != null)
        {
            Localizer.OnLanguageChanged -= _stateChangedHandler;
        }
        _handelDeliveryInvocation.OnAddressSelected -= SafeAddressSelected;
        _handelDeliveryInvocation.OnClearDelivery -= SafeClearDelivery;
        _handelDeliveryInvocation.OnToggleDirection -= SafeToggleDirection;
        _handelDeliveryInvocation.OnNewOrderReceived -= SafeNewOrderReceived;
    }
}
