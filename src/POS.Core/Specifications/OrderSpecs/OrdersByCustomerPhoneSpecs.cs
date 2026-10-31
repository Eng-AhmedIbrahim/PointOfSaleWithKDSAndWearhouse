namespace POS.Core.Specifications.OrderSpecs;

public class OrdersByCustomerPhoneSpecs : BaseSpecifications<Orders>
{
    public OrdersByCustomerPhoneSpecs(string phoneNumber)
        : base(o => o.Phone1 == phoneNumber
        || o.Phone2 == phoneNumber
        || o.TakeawayCustomerPhone == phoneNumber)
    {
        AddOrderByDesc(o => o.OrderDate!);
    }
}
