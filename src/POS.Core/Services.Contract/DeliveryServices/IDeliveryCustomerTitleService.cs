namespace POS.Core.Services.Contract.DeliveryServices;

public interface IDeliveryCustomerTitleService
{
    public Task<IEnumerable<DeliveryTitleToReturnDto>> GetAllAsync();
    public Task<DeliveryTitleToReturnDto?> GetByIdAsync(int id);
    public Task AddAsync(DeliveryTitleDto title);
    public Task UpdateAsync(DeliveryCustomerTitle titleDto);
    public Task DeleteAsync(int id);
    public Task<bool> ExistsAsync(int id);
}