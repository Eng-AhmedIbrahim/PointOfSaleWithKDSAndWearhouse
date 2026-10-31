using System;
using System.Linq.Expressions;
using POS.Core.Entities.DineIn;

namespace POS.Core.Specifications.DineInSpecs;

public class DineInOrderByStateSpec : BaseSpecifications<Orders>
{
    public DineInOrderByStateSpec(string state)
        : base(o => o.OrderType == OrderTypes.DineIn && 
                   (state == "Open" ? o.OrderState == OrderStates.Pending : o.OrderState == OrderStates.Completed))
    {
        Includes.Add(o => o.OrderDetails!);
        AddThenInclude("OrderDetails.OrderItemAttributes");
        AddOrderByDesc(o => o.OrderDate);
        EnableSplitQuery(); // Use split queries for better performance with multiple collections
    }
}
