using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.VoidDtos;

namespace POS.Core.Services.Contract.VoidServices;

public interface IVoidService
{
    Task<bool> VoidOrderAsync(int orderId, string reason, string voidBy, string voidByName, bool returnToStock = false);
    Task<bool> VoidItemsAsync(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName, bool returnToStock = false);

    /// <summary>
    /// Returns a full void history report for a given POS date, all order types.
    /// </summary>
    Task<List<VoidReportDto>> GetVoidReportAsync(DateTime posDate);
}
