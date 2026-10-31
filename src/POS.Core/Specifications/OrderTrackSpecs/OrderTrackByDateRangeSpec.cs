namespace POS.Core.Specifications.OrderTrackSpecs;

public class OrderTrackByDateRangeSpec : BaseSpecifications<OrderTrack>
{
    public OrderTrackByDateRangeSpec(DateTime startDate, DateTime endDate)
        : base(ot => ot.ActionDateTime >= startDate && ot.ActionDateTime <= endDate)
    {
        AddOrderByDesc(ot => ot.ActionDateTime);
    }
}
