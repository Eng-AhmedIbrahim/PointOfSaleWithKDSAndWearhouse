using POS.Contract.Dtos.DineIn;

namespace POS.Core.Services.Contract.DineInOrderServices;

public interface IDineInOrderService
{
    /// <summary>
    /// Creates a new DineIn order in the database
    /// </summary>
    Task<Orders?> CreateDineInOrderAsync(Orders order);
    
    /// <summary>
    /// Updates an existing DineIn order
    /// </summary>
    Task<Orders?> UpdateDineInOrderAsync(Orders order);
    
    /// <summary>
    /// Gets a DineIn order by its database ID
    /// </summary>
    Task<Orders?> GetDineInOrderByIdAsync(int orderId);
    
    /// <summary>
    /// Gets a DineIn order by table ID and state
    /// </summary>
    Task<Orders?> GetDineInOrderByTableIdAsync(int tableId, string state = "Open");

    /// <summary>
    /// Gets all open DineIn orders for a specific table
    /// </summary>
    Task<IReadOnlyList<Orders>> GetOpenOrdersByTableIdAsync(int tableId);
    
    /// <summary>
    /// Gets all open DineIn orders
    /// </summary>
    Task<IReadOnlyList<Orders>> GetAllOpenDineInOrdersAsync();
    
    /// <summary>
    /// Closes a DineIn order
    /// </summary>
    Task<bool> CloseDineInOrderAsync(int orderId, decimal? paid = null, decimal? remain = null);
    
    
    /// <summary>
    /// Adds items to an existing DineIn order
    /// </summary>
    Task<bool> AddItemsToDineInOrderAsync(int dineInOrderId, List<OrderItemsDetails> items);
    
    /// <summary>
    /// Updates discount for a DineIn order
    /// </summary>
    Task<bool> UpdateDineInOrderDiscountAsync(int dineInOrderId, decimal? discountAmount, decimal? discountPercentage, string? discountType, string? discountReason);

    /// <summary>
    /// Transfers a DineIn order to a new table
    /// </summary>
    Task<bool> TransferDineInOrderAsync(int orderId, int newTableId, string newTableName);

    /// <summary>
    /// Merges multiple DineIn orders into a primary order
    /// </summary>
    Task<bool> MergeDineInOrdersAsync(int primaryOrderId, List<int> secondaryOrderIds);

    /// <summary>
    /// Splits an order by moving specific quantities of items to one or more new orders/tables
    /// </summary>
    Task<bool> SplitDineInOrderAsync(int sourceOrderId, List<SplitTargetDto> targets);

    
    Task<int> IncrementPrintCountAsync(int orderId);
    Task<bool> ReserveTableAsync(Orders reservationOrder);
    Task<bool> CancelReservationAsync(int orderId);
    Task<bool> SeatReservationAsync(int orderId, string captainId, string captainName);
}
