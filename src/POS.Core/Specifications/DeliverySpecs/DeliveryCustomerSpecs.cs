namespace POS.Core.Specifications.DeliverySpecs;

public class DeliveryCustomerSpecs : BaseSpecifications<DeliveryCustomerInfo>
{
    public DeliveryCustomerSpecs() 
    {
        AddIncludes();
    }

    public DeliveryCustomerSpecs(int customerId) : base(c => c.Id == customerId)
    {
        AddIncludes();
    }

    public DeliveryCustomerSpecs(string phoneNumber) 
        : base(c => c.FirstPhoneNumber == phoneNumber || c.SecondPhoneNumber == phoneNumber)
    {
        AddIncludes();
    }

    private void AddIncludes()
    {
        Includes.Add(c => c.CustomerAddresses!);
        AddThenInclude("CustomerAddresses.DeliveryZone");
        AddThenInclude("CustomerAddresses.Branch");
    }
}
    