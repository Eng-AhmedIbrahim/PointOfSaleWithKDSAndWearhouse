using System;
using System.Linq.Expressions;
using POS.Core.Entities.DineIn;

namespace POS.Core.Specifications.DineInSpecs;

public class DineInOrderByTableIdSpec : BaseSpecifications<Orders>
{
    public DineInOrderByTableIdSpec(int tableId, string state = "Open")
        : base(o => o.TableID == tableId && 
                   o.OrderType == OrderTypes.DineIn && 
                   (state == "Open" ? o.OrderState == OrderStates.Pending : 
                    state == "Reserved" ? o.OrderState == OrderStates.Reserved : 
                    o.OrderState == OrderStates.Completed))
    {
        Includes.Add(o => o.OrderDetails!);
        AddThenInclude("OrderDetails.OrderItemAttributes");
        AddThenInclude("OrderDetails.MenuSalesItem");
        AddThenInclude("OrderDetails.MenuSalesItem.Category");
        AddOrderByDesc(o => o.OrderDate);
        EnableSplitQuery(); // Use split queries for better performance with multiple collections
    }
}
