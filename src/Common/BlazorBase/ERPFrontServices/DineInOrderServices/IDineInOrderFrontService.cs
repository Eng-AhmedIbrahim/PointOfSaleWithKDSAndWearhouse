using System.Threading.Tasks;
using System.Collections.Generic;
using POS.Contract.Dtos.DineIn;

namespace BlazorBase.ERPFrontServices.DineInOrderServices;

public interface IDineInOrderFrontService
{
    Task<DineInOrderDto?> CreateDineInOrderAsync(DineInOrderDto order);
    Task<DineInOrderDto?> UpdateDineInOrderAsync(DineInOrderDto order);
    Task<DineInOrderDto?> GetDineInOrderByIdAsync(int orderId);
    Task<DineInOrderDto?> GetDineInOrderByTableIdAsync(int tableId, string state = "Open");
    Task<IReadOnlyList<DineInOrderDto>> GetOpenOrdersByTableIdAsync(int tableId);
    Task<IReadOnlyList<DineInOrderDto>> GetAllOpenDineInOrdersAsync();
    Task<bool> CloseDineInOrderAsync(int orderId, decimal? paid = null, decimal? remain = null);
    Task<bool> VoidDineInOrderAsync(int orderId, string reason, string voidBy, string voidByName);
    Task<bool> AddItemsToDineInOrderAsync(int dineInOrderId, List<OrderItemsDetailsDto> items);
    Task<bool> UpdateDineInOrderDiscountAsync(int dineInOrderId, decimal? discountAmount, decimal? discountPercentage, string? discountType, string? discountReason);
    Task<bool> TransferDineInOrderAsync(int orderId, int newTableId, string newTableName);
    Task<bool> MergeDineInOrdersAsync(int primaryOrderId, List<int> secondaryOrderIds);
    Task<bool> SplitDineInOrderAsync(int sourceOrderId, List<SplitTargetDto> targets);
    Task<bool> VoidDineInItemsAsync(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName);
    Task<int> IncrementPrintCountAsync(int orderId);
    Task<bool> ReserveTableAsync(int tableId, DineInOrderDto reservationDetails);
    Task<bool> CancelReservationAsync(int tableId);
    Task<bool> SeatReservationAsync(int orderId, string captainId, string captainName);
}
