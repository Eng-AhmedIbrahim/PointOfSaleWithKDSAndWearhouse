namespace POS.Core.Services.Contract.DeliveryServices;

public interface IDeliveryCustomerService
{
    public Task<DeliveryCustomerInfo?> GetCustomerByIdAsync(int customerId);
    public Task<DeliveryCustomerInfo?> GetCustomerWithAddressesAsync(int customerId);
    public Task<DeliveryCustomerInfo?> GetCustomerByPhoneNumberAsync(string phoneNumber);
    public Task<IReadOnlyList<DeliveryCustomerInfo>> GetAllCustomersAsync();
    public Task<DeliveryCustomerInfo> CreateCustomerAsync(DeliveryCustomerInfo customer);
    public Task<DeliveryCustomerInfo> AddNewCustomerAddressAsync(string firstPhoneNumber, CustomerAddress customerAddress);
    public Task<DeliveryCustomerInfo> UpdateCustomerAsync(DeliveryCustomerInfo customer);
    public Task<bool> DeleteCustomerAsync(int customerId);
}