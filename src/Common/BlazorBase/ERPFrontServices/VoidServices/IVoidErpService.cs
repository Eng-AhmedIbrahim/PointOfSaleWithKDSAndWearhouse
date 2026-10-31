using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.VoidDtos;

namespace BlazorBase.ERPFrontServices.VoidServices;

public interface IVoidErpService
{
    Task<bool> VoidOrder(int orderId, string reason, string voidBy, string voidByName, bool returnToStock = false);
    Task<bool> VoidItems(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName, bool returnToStock = false);
    Task<List<VoidReportDto>> GetVoidReport(DateTime posDate);
}
