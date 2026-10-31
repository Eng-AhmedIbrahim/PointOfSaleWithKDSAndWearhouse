using POS.Core.Entities.ReservationEntity;

namespace POS.Core.Specifications.ReservationSpecs;

public class ReservationByOrderIdSpec : BaseSpecifications<Reservation>
{
    public ReservationByOrderIdSpec(int orderId) : base(r => r.OrderId == orderId)
    {
    }
}
