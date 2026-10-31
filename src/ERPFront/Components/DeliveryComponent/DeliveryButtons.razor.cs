namespace ERPFront.Components.DeliveryComponent;

public partial class DeliveryButtons
{
    private async Task AddCustomer()
    {
        var client = await _deliveryServices.GetClientByPhoneNumberAsync(_commonProperties!.CustomerDetails!.FirstPhoneNumber!);
        if (client.Id == 0)
        {
            var customerDetails = BackupCustomer() ?? new();
            var createdCustomer = await _deliveryServices.CreateClientAsync(customerDetails);
            if (createdCustomer is not null)
            {
                SetPosModeToDeliveryMode();
                return;
            }
            else
            {
                _snackbar.Add("Customer Not Created", Severity.Error);
                return;
            }
        }

        SetPosModeToDeliveryMode();
    }

    private void BackToPos()
    {
        _navigationManager.NavigateTo("/pos");
        _commonProperties.AppendedTableItems!.Clear();
        _commonProperties.CustomerDetails = new();
        _commonProperties.CurrentPosMode = PosModes.TakeAway.ToString();
    }


    private void SetPosModeToDeliveryMode()
    {
        _commonProperties.CurrentPosMode = PosModes.Delivery.ToString();

        _handelDeliveryInvocation.DeliveryDetails = $"(Phone : {_commonProperties.CustomerDetails!.FirstPhoneNumber} $_$_$ Name : {_commonProperties.CustomerDetails!.CustomerName})";
        _navigationManager.NavigateTo("/pos");
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
            AddressNote = _commonProperties.CustomerDetails!.AddressNote
        };

    private void ClearDeliveryOrder()
    {
        _commonProperties.CustomerDetails = new();
        _handelDeliveryInvocation.CustomerDetails = new();
        _handelDeliveryInvocation.TriggerAddressSelected();
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
}