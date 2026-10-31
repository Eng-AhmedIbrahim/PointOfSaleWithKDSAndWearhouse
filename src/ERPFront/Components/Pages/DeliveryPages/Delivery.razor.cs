namespace ERPFront.Components.Pages.DeliveryPages;

public partial class Delivery : IDisposable
{
    public IReadOnlyList<DeliveryTitleToReturnDto>? CustomerTitles { get; set; } = new List<DeliveryTitleToReturnDto>();
    public IReadOnlyList<BranchToReturnDto>? Branches { get; set; } = new List<BranchToReturnDto>();

    private IReadOnlyList<DeliveryZonesToReturnDto> Zones = [];
    protected async override Task OnInitializedAsync()
    {
        _handelDeliveryInvocation.OnAddressSelected += HandleAddressSelected;
        _commonProperties.CustomerDetails = new ();

        await GetAllTitles();
        await GetAllBranches();
        await GetAllZones();
    }

    private async Task GetAllBranches()
         => Branches = await _branchService.GetBranches();

    private async Task GetAllZones()
    {
        var zonesToReturnDtos = await _deliveryServices.GetAllDeliveryZonesAsync();
        Zones = zonesToReturnDtos.ToList();
    }

    private void OnBranchChanged(string branchName)
    {
        _commonProperties!.CustomerDetails!.BranchName = branchName;
        if (Branches != null)
        {
            var branch = Branches.FirstOrDefault(b => b.Name == branchName);
            if (branch != null)
            {
                _commonProperties.CustomerDetails.BranchId = branch.Id;
                _commonProperties.CustomerDetails.BranchApiUrl = branch.ApiUrl;
            }
        }
    }

    private void OnZoneChanged(string zoneName)
    {
        _commonProperties!.CustomerDetails!.ZoneName = zoneName;
        var zone = Zones?.FirstOrDefault(z => z.ZoneName == zoneName);
        if (zone != null)
        {
            _commonProperties.CustomerDetails.ZoneID = zone.Id;
            _commonProperties.CustomerDetails.BranchId = zone.BranchId;
            _commonProperties.CustomerDetails.ZoneFees = zone.DeliveryFee ?? 0m;
            _commonProperties.CustomerDetails.ZoneBonus = zone.ZoneBonus ?? 0m;

            
            if (Branches != null)
            {
                var branch = Branches.FirstOrDefault(b => b.Id == zone.BranchId);
                if (branch != null)
                {
                    _commonProperties.CustomerDetails.BranchName = branch.Name;
                    _commonProperties.CustomerDetails.BranchApiUrl = branch.ApiUrl;
                }
            }
        }
    }

    private async Task GetAllTitles()
       => CustomerTitles = await _deliveryServices.GetAllDeliveryCustomerTitlesAsync();

    private void HandleAddressSelected()
        => StateHasChanged();


    public void Dispose()
        => _handelDeliveryInvocation.OnAddressSelected -= HandleAddressSelected;
}
