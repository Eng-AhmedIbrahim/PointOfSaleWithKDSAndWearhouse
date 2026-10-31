namespace POS.Core.Services.Contract.OrderServices;


public interface IOrderService
{
    public Task<Orders?> CreateOrderAsync(Orders order);

    public Task<IReadOnlyList<OrderSetting>> GetOrderSettingsAsync(string? computerName = null);
    public Task<OrderSetting?> UpdateOrderSettingAsync(OrderTypes orderType, OrderSetting orderSetting, string? computerName = null);
    public Task<OrderSetting?> GetOrderSettingAsync(OrderTypes orderType, string? computerName = null);
    public Task<Orders?> GetOrderByIdAsync(int id);
    public Task<Orders?> GetOrderByCallCenterIdAsync(int callCenterOrderId);
    
    public Task<bool> UpdateOrderStatusAsync(int id, OrderStates state);
    public Task<IReadOnlyList<Orders>?> GetFailedDeliveryOrdersAsync();
    public Task<OrderDto?> GetOrderDtoByIdAsync(int id);
    public Task<bool> UpdateOrderAsync(Orders order);
    public Task<Orders?> FullUpdateOrderAsync(Orders order);
    public Task<IReadOnlyList<Orders>> GetOrdersByCustomerPhoneAsync(string phoneNumber);
    public Task<int> IncrementPrintCountAsync(int id);
}