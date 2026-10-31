using POS.Contract.Dtos.OrderDto;
using POS.Contract.Dtos.OrderDtos;

namespace POS.Core.Services.Contract.OrderApiServices;

public interface IOrderApiServices
{
    public Task<List<string>> GetBranchDetails(OrderDto orderDto);
}
