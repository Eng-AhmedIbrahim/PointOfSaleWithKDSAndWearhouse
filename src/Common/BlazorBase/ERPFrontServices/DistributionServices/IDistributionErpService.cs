using POS.Contract.Dtos.DineIn;

namespace BlazorBase.ERPFrontServices.DistributionServices;

public interface IDistributionErpService
{
    public Task<ICollection<UserToReturnDto>> GetDeliveryUsers();
    public Task<ICollection<OrderDto>> GetUnCompletedDeliveryOrders();
    public Task<OrderDto?> DispatchOrder(OrderDto orderDto);
    public Task<OrderDto?> CollectDeliveryOrder(OrderDto orderDto);
    public Task<bool> UnDispatchOrder(int orderId);
    public Task<bool> CollectDriverOrders(string driverId);
    public Task<bool> CollectAllOrders();
    public Task<List<DriverSettlementDto>> GetDriverSettlement(DateTime posDate);
    public Task<bool> VoidOrder(int orderId, string reason, string voidBy, string voidByName);
    public Task<bool> VoidItems(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName);
    public Task<List<OrderDto>> GetVoidedOrders(DateTime posDate);
}