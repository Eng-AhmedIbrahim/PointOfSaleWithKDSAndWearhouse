using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using POS.Core.Entities.OrderEntity;
using POS.Core.Services.Contract.OrderTrackServices;
using POS.Core.Specifications.OrderTrackSpecs;
using Serilog;

namespace POS.Services.OrderTrackServices;

public class OrderTrackService : IOrderTrackService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderTrackService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> TrackOrderActionAsync(OrderTrack orderTrack)
    {
        if (orderTrack is null)
            return false;

        try
        {
            orderTrack.ActionDateTime = DateTime.Now;
            orderTrack.MachineName = string.IsNullOrEmpty(orderTrack.MachineName) 
                ? Environment.MachineName 
                : orderTrack.MachineName;

            await _unitOfWork.Repository<OrderTrack>().AddAsync(orderTrack);
            var result = await _unitOfWork.CompleteAsync();
            
            return result > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while tracking order action.");
            return false;
        }
    }

    public async Task<IReadOnlyList<OrderTrack>> GetOrderTrackingHistoryAsync(int orderId)
    {
        try
        {
            var spec = new OrderTrackByOrderIdSpec(orderId);
            return await _unitOfWork.Repository<OrderTrack>().GetAllWithSpecificationAsync(spec);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while getting order tracking history.");
            return new List<OrderTrack>();
        }
    }

    public async Task<IReadOnlyList<OrderTrack>> GetOrderTrackingByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var spec = new OrderTrackByDateRangeSpec(startDate, endDate);
            return await _unitOfWork.Repository<OrderTrack>().GetAllWithSpecificationAsync(spec);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while getting order tracking by date range.");
            return new List<OrderTrack>();
        }
    }
}
