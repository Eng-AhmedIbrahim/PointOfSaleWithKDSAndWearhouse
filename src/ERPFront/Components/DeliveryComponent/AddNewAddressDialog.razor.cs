using BlazorBase.Models.DeliveryModels;

namespace ERPFront.Components.DeliveryComponent;

public partial class AddNewAddressDialog
{
    public IReadOnlyList<BranchToReturnDto>? Branches { get; set; } = new List<BranchToReturnDto>();

    private IReadOnlyList<DeliveryZonesToReturnDto> Zones = [];

    private bool IsBranchSelected = false;

    private void Cancel()
    { 
        _handelDeliveryInvocation.CustomerDetails = new();
        ClearDialogReference();

    }

    private async Task Submit()
    {
        await AddNewAddress();
    }

    private async Task AddNewAddress()
    {
        CustomerNewAddressDto customerAddress = GetNewCustomerAddressData();
         var result = await _deliveryServices.AddNewCustomerAddressAsync(customerAddress);
        if(result.FirstPhoneNumber == string.Empty)
        {
            _snackbar.Add("Customer Address Not Created", Severity.Error);
            return;
        }

        _commonProperties.CustomerDetails!.FirstPhoneNumber = _handelDeliveryInvocation.CustomerDetails!.FirstPhoneNumber;
        _commonProperties.CustomerDetails.SecondPhoneNumber = _handelDeliveryInvocation.CustomerDetails.SecondPhoneNumber;
        _commonProperties.CustomerDetails.ClientTitle = _handelDeliveryInvocation.CustomerDetails.ClientTitle;
        _commonProperties.CustomerDetails.CustomerName = _handelDeliveryInvocation.CustomerDetails.CustomerName;

        _handelDeliveryInvocation.CustomerDetails = new();
        _handelDeliveryInvocation.TriggerAddressSelected();
        Cancel();
    }

    private CustomerNewAddressDto GetNewCustomerAddressData()
        => new CustomerNewAddressDto()
        {
            FirstPhoneNumber = _handelDeliveryInvocation.CustomerDetails!.FirstPhoneNumber,
            BranchName = _commonProperties.CustomerDetails!.BranchName,
            ZoneName = _commonProperties.CustomerDetails.ZoneName,
            ClientAddress = _commonProperties.CustomerDetails.ClientAddress,
            AddressNote = _commonProperties.CustomerDetails.AddressNote,
            FlatNumber = _commonProperties.CustomerDetails.FlatNumber,
            FloorNumber = _commonProperties.CustomerDetails.FloorNumber,
            HomeNumber = _commonProperties.CustomerDetails.HomeNumber,
        };

    private void ClearDialogReference()
    {
        _commonProperties.DialogReference?.Close();
        _commonProperties.DialogReference = null;
    }

    private async Task OnValueChanged(string selectedBranch)
    {
        IsBranchSelected = true;
        _commonProperties!.CustomerDetails!.BranchName = selectedBranch;
        var branchId = Branches!.Where(n => n.Name == selectedBranch).Select(b => b.Id).FirstOrDefault();
        var zonesToReturnDtos = await _deliveryServices.GetDeliveryZoneByBranchAsync(branchId);
        Zones = zonesToReturnDtos.ToList();
    }


    private Task<IEnumerable<string>> SearchZones(string value, CancellationToken token)
    {

        var result = string.IsNullOrEmpty(value)
            ? Zones.Select(x => x.ZoneName ?? string.Empty)
            : Zones.Where(x => x.ZoneName != null && x.ZoneName.Contains(value, StringComparison.OrdinalIgnoreCase))
                            .Select(x => x.ZoneName ?? string.Empty);

        return Task.FromResult(result);
    }



    protected async override Task OnInitializedAsync()
    {
        _commonProperties.CustomerDetails = new();

        await GetAllBranches();
    }

    private async Task GetAllBranches()
         => Branches = await _branchService.GetBranches();

}
