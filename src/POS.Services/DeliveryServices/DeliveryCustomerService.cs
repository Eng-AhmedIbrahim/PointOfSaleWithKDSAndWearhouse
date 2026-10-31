using POS.Core.Entities.Delivery;
using POS.Core.Specifications.BranchSpecs;

namespace POS.Services.DeliveryServices;

public class DeliveryCustomerService : IDeliveryCustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    public DeliveryCustomerSpecs _deliveryCustomerSpecs = new DeliveryCustomerSpecs();

    public DeliveryCustomerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<DeliveryCustomerInfo?> GetCustomerByIdAsync(int customerId)
    {
        _deliveryCustomerSpecs = new DeliveryCustomerSpecs(customerId);
        return await _unitOfWork.Repository<DeliveryCustomerInfo>()
            .GetByIdWithSpecificationAsync(_deliveryCustomerSpecs);
    }

    public async Task<DeliveryCustomerInfo?> GetCustomerWithAddressesAsync(int customerId)
    {
        _deliveryCustomerSpecs = new DeliveryCustomerSpecs(customerId);

        return await _unitOfWork.Repository<DeliveryCustomerInfo>()
           .GetByIdWithSpecificationAsync(_deliveryCustomerSpecs);
    }

    public async Task<DeliveryCustomerInfo?> GetCustomerByPhoneNumberAsync(string phoneNumber)
    {
        _deliveryCustomerSpecs = new DeliveryCustomerSpecs(phoneNumber);
        return await _unitOfWork.Repository<DeliveryCustomerInfo>()
            .GetByIdWithSpecificationAsync(_deliveryCustomerSpecs);
    }

    public async Task<IReadOnlyList<DeliveryCustomerInfo>> GetAllCustomersAsync()
    {
        _deliveryCustomerSpecs = new DeliveryCustomerSpecs();

        return await _unitOfWork.Repository<DeliveryCustomerInfo>()
            .GetAllWithSpecificationAsync(_deliveryCustomerSpecs);
    }

    public async Task<DeliveryCustomerInfo> CreateCustomerAsync(DeliveryCustomerInfo customer)
    {
        var address = customer.CustomerAddresses!.First();
        
        if (address.BranchId == 0 || address.BranchId == null)
        {
            if (!string.IsNullOrEmpty(address.BranchName))
            {
                var branchId = await GetBranchIdAsync(address.BranchName!);
                if (branchId > 0) address.BranchId = branchId;
                else throw new Exception($"Branch '{address.BranchName}' not found.");
            }
            else
            {
                 throw new Exception("Branch is required to create a customer address.");
            }
        }

        if (address.DeliveryZoneId == 0 || address.DeliveryZoneId == null)
        {
            if (!string.IsNullOrEmpty(address.ZoneName))
            {
                var zoneId = await GetZoneIdAsync(address.ZoneName!);
                if (zoneId > 0) address.DeliveryZoneId = zoneId;
                else throw new Exception($"Zone '{address.ZoneName}' not found.");
            }
            else
            {
                 throw new Exception("Delivery Zone is required to create a customer address.");
            }
        }

        await _unitOfWork.Repository<DeliveryCustomerInfo>().AddAsync(customer);
        await _unitOfWork.CompleteAsync();
        return customer;
    }

    public async Task<DeliveryCustomerInfo> UpdateCustomerAsync(DeliveryCustomerInfo customer)
    {
        var existingCustomer = await _unitOfWork.Repository<DeliveryCustomerInfo>()
            .GetByIdAsync(customer.Id);

        if (existingCustomer == null)
            throw new KeyNotFoundException("Customer not found");

        existingCustomer.FirstPhoneNumber = customer.FirstPhoneNumber;
        existingCustomer.SecondPhoneNumber = customer.SecondPhoneNumber;
        existingCustomer.ClientTitle = customer.ClientTitle;
        existingCustomer.CustomerName = customer.CustomerName;

        _unitOfWork.Repository<DeliveryCustomerInfo>().Update(existingCustomer);
        await _unitOfWork.CompleteAsync();
        return existingCustomer;
    }

    public async Task<bool> DeleteCustomerAsync(int customerId)
    {
        var customer = await _unitOfWork.Repository<DeliveryCustomerInfo>().GetByIdAsync(customerId);
        if (customer == null)
            return false;

        _unitOfWork.Repository<DeliveryCustomerInfo>().Delete(customer);
        await _unitOfWork.CompleteAsync();
        return true;
    }

    public async Task<DeliveryCustomerInfo> AddNewCustomerAddressAsync(string firstPhoneNumber,CustomerAddress customerAddress)
    {

        var customer = await GetCustomerByPhoneNumberAsync(firstPhoneNumber);
        if (customer == null)
            return null!;

        customerAddress.DeliveryCustomerId = customer.Id;
        customerAddress.BranchId = await GetBranchIdAsync(customerAddress.BranchName!);
        customerAddress.DeliveryZoneId = await GetZoneIdAsync(customerAddress.ZoneName!);

        await _unitOfWork.Repository<CustomerAddress>().AddAsync(customerAddress);
        var result = await _unitOfWork.CompleteAsync();
        if(result <= 0)
        {
            Log.Error("Failed to add new address for customer");
            return null!;
        }

        return customer;
    }

    private async Task<int> GetBranchIdAsync(string branchName)
    {
        var branchSpecs = new BranchSpecs(branchName);
        var branches = await _unitOfWork.Repository<Branch>().GetAllWithSpecificationAsync(branchSpecs);
        return branches.FirstOrDefault()?.Id ?? 0;
    }


    private async Task<int> GetZoneIdAsync(string zoneName)
    {
        var zoneSpecs = new DeliveryZonesSpecs(zoneName);
        var zones = await _unitOfWork.Repository<DeliveryZone>().GetAllWithSpecificationAsync(zoneSpecs);
        return zones.FirstOrDefault()?.Id ?? 0;
    }
}