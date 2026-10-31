using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.DineIn;

namespace POS.Core.Services.Contract.DistributionServices;

public interface IDistributionServices
{
    public Task<ICollection<Orders>> GetUnCompletedDeliveryOrdersAsync();
    public Task<bool> UpdateDeliveryOrdersAfterDispatchAsync(OrderDto orderDto);
    public Task<bool> UnDispatchOrderAsync(int orderId);
    public Task<ICollection<Orders>> GetOrdersByDriverAsync(string driverId);
    public Task<List<DriverSettlementDto>> GetDriverSettlementAsync(DateTime posDate);
    public Task<ICollection<Orders>> GetVoidedDeliveryOrdersAsync(DateTime posDate);
}