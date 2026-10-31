namespace POS.Core.Specifications.OrderSpecs;

public class OrderSettingSpecs:BaseSpecifications<OrderSetting>
{
    public OrderSettingSpecs(OrderTypes orderTypes, string? computerName = null)
        : base(x => x.OrderType == orderTypes.ToString() 
        && (string.IsNullOrEmpty(computerName) || x.ComputerName == computerName))
    {
    }

    public OrderSettingSpecs(string computerName)
        : base(x => x.ComputerName == computerName)
    {
    }
}