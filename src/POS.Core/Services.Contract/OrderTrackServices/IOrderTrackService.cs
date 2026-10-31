namespace POS.Core.Services.Contract.OrderTrackServices;

public interface IOrderTrackService
{
    /// <summary>
    /// Tracks an order action
    /// </summary>
    Task<bool> TrackOrderActionAsync(OrderTrack orderTrack);
    
    /// <summary>
    /// Gets order tracking history for a specific order
    /// </summary>
    Task<IReadOnlyList<OrderTrack>> GetOrderTrackingHistoryAsync(int orderId);
    
    /// <summary>
    /// Gets all order tracking records within a date range
    /// </summary>
    Task<IReadOnlyList<OrderTrack>> GetOrderTrackingByDateRangeAsync(DateTime startDate, DateTime endDate);
}
