using POS.Contract.Dtos.OrderDtos;

namespace POS.Core.Services.Contract.OrderApiServices;

public interface IDeliveryOrderApiServices
{
    public void BackupDeliveryOrder(OrderDto OrderDto, Orders order);
    public Task<string> GenerateAndPrintDeliveryReceipts(OrderDto Order, Orders createdOrder, List<string> branchDetails, bool isClosed = false);
}