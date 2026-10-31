using POS.Core.Entities.OrderEntity;

namespace POS.Core.Specifications.OrderSpecs;

public class OrdersByCallCenterIdSpecs : BaseSpecifications<Orders>
{
    public OrdersByCallCenterIdSpecs(int callCenterOrderId) 
        : base(x => x.CallCenterOrderId == callCenterOrderId)
    {
        Includes.Add(x => x.OrderDetails!);
        AddThenInclude("OrderDetails.OrderItemAttributes");
        AddThenInclude("OrderDetails.OrderItemComments");
        AddThenInclude("OrderDetails.MenuSalesItem");
        AddThenInclude("OrderDetails.MenuSalesItem.Category");
    }
}
