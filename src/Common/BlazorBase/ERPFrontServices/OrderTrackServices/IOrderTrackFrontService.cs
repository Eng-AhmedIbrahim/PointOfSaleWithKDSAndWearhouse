using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using POS.Contract.Dtos.OrderDtos;

namespace BlazorBase.ERPFrontServices.OrderTrackServices;

public interface IOrderTrackFrontService
{
    Task<bool> TrackOrderActionAsync(OrderTrackDto orderTrack);
    Task<IReadOnlyList<OrderTrackDto>> GetOrderTrackingHistoryAsync(int orderId);
    Task<IReadOnlyList<OrderTrackDto>> GetOrderTrackingByDateRangeAsync(DateTime startDate, DateTime endDate);
}
