namespace ERPFront.Components.DeliveryComponent;

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

        // Populate essential IDs and Fees
        _commonProperties.CustomerDetails.BranchId = address.BranchId;
        _commonProperties.CustomerDetails.ZoneID = address.DeliveryZoneId;
        _commonProperties.CustomerDetails.ZoneFees = address.DeliveryFee;
        _commonProperties.CustomerDetails.ZoneBonus = address.ZoneBonus;
        _commonProperties.CustomerDetails.CustomerAddressId = address.Id;
        _commonProperties.CustomerDetails.Id = address.DeliveryCustomerId;

        // Populate Branch API URL if available
        var branches = await _branchService.GetBranches();
        var branch = branches.FirstOrDefault(b => b.Id == address.BranchId);
        _commonProperties.CustomerDetails.BranchApiUrl = branch?.ApiUrl;

        _handelDeliveryInvocation.TriggerAddressSelected();

        ClearDialogReference();
    }


    private void Cancel()
        => ClearDialogReference();


    private void ClearDialogReference()
    {
        _commonProperties.DialogReference?.Close();
        _commonProperties.DialogReference = null;
    }
}
