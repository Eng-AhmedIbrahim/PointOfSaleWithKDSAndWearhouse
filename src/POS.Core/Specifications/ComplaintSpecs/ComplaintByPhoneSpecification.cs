using POS.Core.Entities.ComplaintEntity;

namespace POS.Core.Specifications.ComplaintSpecs;

public class ComplaintByPhoneSpecification : BaseSpecifications<Complaint>
{
    public ComplaintByPhoneSpecification(string phone) 
        : base(x => x.CustomerPhone == phone)
    {
        AddOrderByDesc(x => x.ComplaintDate);
    }
}
