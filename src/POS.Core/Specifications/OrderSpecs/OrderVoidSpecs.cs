using POS.Core.Entities.OrderEntity;

namespace POS.Core.Specifications.OrderSpecs;

public class OrderVoidSpecs : BaseSpecifications<OrderVoid>
{
    public OrderVoidSpecs(DateTime dateStart, DateTime dateEnd)
        : base(v => v.VoidDate >= dateStart && v.VoidDate < dateEnd)
    {
        Includes.Add(v => v.Order!);
        Includes.Add(v => v.VoidItems);
        AddThenInclude("VoidItems.OrderItem");
    }
}
