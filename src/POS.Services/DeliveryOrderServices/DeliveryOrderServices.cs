namespace POS.Services.DeliveryOrderServices;
using POS.Core.Services.Contract.AppDateServices;

internal class DeliveryOrderServices : IDeliveryOrderServices
{

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppDateService _appDateService;

    public DeliveryOrderServices(IUnitOfWork unitOfWork, IAppDateService appDateService)
    {
        _unitOfWork = unitOfWork;
        _appDateService = appDateService;
    }

    public async Task<Orders?> CreateDeliveryOrderAsync(Orders order)
    {
        if (order is null)
            return null;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var appDate = await _appDateService.UpdateOrderNumber();
                order.OrderID = appDate.CurrentOrderNumber;
                order.OrderDate = appDate.PosDate.Date.Add(DateTime.Now.TimeOfDay);

                if (string.IsNullOrEmpty(order.MachineName))
                {
                    order.MachineName = Environment.MachineName;
                }

                await _unitOfWork.Repository<Orders>().AddAsync(order);

                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    foreach (var item in order.OrderDetails)
                    {
                        await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(item);
                    }
                }

                var result = await _unitOfWork.CompleteAsync();
                if (result <= 0)
                {
                    transaction.Rollback();
                    return null;
                }

                transaction.Commit();
                return order;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while creating the order.");
                return null;
            }
        }
    }
}
