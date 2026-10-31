namespace POS.Core.Specifications.OrderSpecs;

public class OrdersByIdSpecs : BaseSpecifications<Orders>
{
    public OrdersByIdSpecs(int id)
        : base(o => o.Id == id)
    {
        Includes.Add(o => o.OrderDetails!);
        AddThenInclude("OrderDetails.OrderItemAttributes");
        AddThenInclude("OrderDetails.OrderItemComments");
        AddThenInclude("OrderDetails.MenuSalesItem");
        AddThenInclude("OrderDetails.MenuSalesItem.Category");
    }
}
