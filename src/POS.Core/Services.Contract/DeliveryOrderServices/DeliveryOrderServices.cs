namespace POS.Core.Services.Contract.DeliveryOrderServices;

public interface IDeliveryOrderServices
{
    public Task<Orders?> CreateDeliveryOrderAsync(Orders order);

}
