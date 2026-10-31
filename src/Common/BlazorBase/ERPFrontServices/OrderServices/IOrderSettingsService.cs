using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.KitchenDtos;

namespace BlazorBase.ERPFrontServices.OrderServices;

public interface IOrderSettingsService
{
    //Task<Order> CreateOrder(Order order);

    public Task<ICollection<OrderSettingToReturnDto>?> GetOrderSettingsAsync(string? computerName = null);
    public Task<OrderSettingToReturnDto?> GetOrderSettingAsync(int orderType, string? computerName = null);
    public Task<OrderSettingToReturnDto?> UpdateOrderSettingAsync(int orderType, OrderSettingToReturnDto dto, string? computerName = null);
    public Task<List<POS.Contract.Dtos.AccountDtos.RoleToReturnDto>?> GetRolesAsync();
    public Task<OrderDto?> CreateOrderAsync(OrderDto orderDto);
    public Task<OrderDto?> UpdateOrderAsync(OrderDto orderDto);
    public Task<OrderDto?> GetOrderByIdAsync(int orderId);
    public Task<int> IncrementPrintCountAsync(int orderId);

    // Kitchen & Printer Management
    public Task<List<KitchenTypeToReturnDto>?> GetAllKitchenTypesAsync();
    public Task<KitchenTypeToReturnDto?> CreateKitchenTypeAsync(KitchenTypeDto dto);
    public Task<bool> UpdateKitchenTypeAsync(int id, KitchenTypeDto dto);
    public Task<bool> DeleteKitchenTypeAsync(int id);

    public Task<List<KitchenPrintersToReturnDto>?> GetAllKitchenPrintersAsync();
    public Task<KitchenPrintersToReturnDto?> CreateKitchenPrinterAsync(KitchenPrintersDto dto);
    public Task<bool> UpdateKitchenPrinterAsync(int id, KitchenPrintersDto dto);
    public Task<bool> DeleteKitchenPrinterAsync(int id);
    public Task<List<string>?> GetInstalledPrintersAsync();
}