namespace POS.Desktop.Components.DeliveryComponent;

public partial class SelectAddressDialog
{
    [Parameter] 
    public string CustomerName { get; set; } = string.Empty;
    [Parameter] 
    public string PhoneNumber { get; set; } = string.Empty;
    [Parameter] 
    public List<CustomerAddressDto> Addresses { get; set; } = new();

    private async Task SelectAddress(CustomerAddressDto address)
    {
        var customer = await _deliveryServices.GetClientByPhoneNumberAsync(_commonProperties.CustomerDetails!.FirstPhoneNumber!);

        _commonProperties.CustomerDetails!.SecondPhoneNumber = customer.SecondPhoneNumber;
        _commonProperties.CustomerDetails!.ClientTitle = customer.ClientTitle;
        _commonProperties.CustomerDetails!.CustomerName = customer.CustomerName;

        _commonProperties.CustomerDetails!.BranchName = address.BranchName;
        _commonProperties.CustomerDetails.ZoneName = address.ZoneName;

        _commonProperties.CustomerDetails.ClientAddress = address.ClientAddress;
        _commonProperties.CustomerDetails.AddressNote = address.AddressNote;
        _commonProperties.CustomerDetails.HomeNumber = address.HomeNumber;
        _commonProperties.CustomerDetails.FloorNumber = address.FloorNumber;
        _commonProperties.CustomerDetails.FlatNumber = address.FlatNumber;
        _commonProperties.CustomerDetails.ZoneID = address.DeliveryZoneId;
        _commonProperties.CustomerDetails.BranchId = address.BranchId;
        _commonProperties.CustomerDetails.ZoneFees = address.DeliveryFee;
        _commonProperties.CustomerDetails.ZoneBonus = address.ZoneBonus;
        _commonProperties.CustomerDetails.CustomerAddressId = address.Id;

        _handelDeliveryInvocation.TriggerAddressSelected();

        ClearDialogReference();
    }

    private void Cancel()
        => ClearDialogReference();


    private async Task AddNew()
    {
        _commonProperties.DialogReference?.Close();
        _commonProperties.DialogReference = null;

        if (_commonProperties.CustomerDetails == null) _commonProperties.CustomerDetails = new();
        
        // Sync with existing phone number
        _handelDeliveryInvocation.CustomerDetails = BlazorBase.Models.DeliveryModels.CustomerDetails.Clone(_commonProperties.CustomerDetails!);
        
        // Initializing fields for NEW address entering
        _commonProperties.CustomerDetails!.ClientAddress = string.Empty;
        _commonProperties.CustomerDetails.FloorNumber = string.Empty;
        _commonProperties.CustomerDetails.FlatNumber = string.Empty;
        _commonProperties.CustomerDetails.HomeNumber = string.Empty;
        _commonProperties.CustomerDetails.AddressNote = string.Empty;
        _commonProperties.CustomerDetails.BranchName = string.Empty;
        _commonProperties.CustomerDetails.ZoneName = string.Empty;
        _commonProperties.CustomerDetails.ZoneID = 0;
        _commonProperties.CustomerDetails.BranchId = 0;
        _commonProperties.CustomerDetails.ZoneFees = 0;
        _commonProperties.CustomerDetails.ZoneBonus = 0;

        _commonProperties.DialogReference = await DialogService.ShowAsync<AddNewAddressDialog>(Localizer["AddNewAddress"]);
    }

    private void ClearDialogReference()
    {
        _commonProperties.DialogReference?.Close();
        _commonProperties.DialogReference = null;
    }
}
