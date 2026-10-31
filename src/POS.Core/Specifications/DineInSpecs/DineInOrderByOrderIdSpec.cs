using System;
using System.Linq.Expressions;
using POS.Core.Entities.DineIn;

namespace POS.Core.Specifications.DineInSpecs;

public class DineInOrderByOrderIdSpec : BaseSpecifications<Orders>
{
    public DineInOrderByOrderIdSpec(int orderId)
        : base(o => o.OrderID == orderId && o.OrderType == OrderTypes.DineIn)
    {
        Includes.Add(o => o.OrderDetails!);
        AddThenInclude("OrderDetails.OrderItemAttributes");
    }
}
