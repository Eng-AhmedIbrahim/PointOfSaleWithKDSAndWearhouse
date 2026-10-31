using System;
using System.Linq.Expressions;
using POS.Core.Entities.DineIn;
using POS.Core.Entities.OrderEntity;

namespace POS.Core.Specifications.DineInSpecs;

public class DineInOrderByIdSpec : BaseSpecifications<Orders>
{
    public DineInOrderByIdSpec(int id)
        : base(o => o.Id == id && o.OrderType == OrderTypes.DineIn)
    {
        Includes.Add(o => o.OrderDetails!);
        AddThenInclude("OrderDetails.OrderItemAttributes");
        AddThenInclude("OrderDetails.OrderItemComments");
        AddThenInclude("OrderDetails.MenuSalesItem");
        AddThenInclude("OrderDetails.MenuSalesItem.Category");
        EnableSplitQuery(); // Use split queries for better performance with multiple collections
    }
}
