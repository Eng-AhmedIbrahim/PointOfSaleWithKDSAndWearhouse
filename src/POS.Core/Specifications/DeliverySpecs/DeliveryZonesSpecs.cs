namespace POS.Core.Specifications.DeliverySpecs;

public class DeliveryZonesSpecs : BaseSpecifications<DeliveryZone>
{
    public DeliveryZonesSpecs(int branchId) : base(x => x.BranchId == branchId) { }

    public DeliveryZonesSpecs(string zoneName) : base(x => x.ZoneName == zoneName)
    {
    }
}