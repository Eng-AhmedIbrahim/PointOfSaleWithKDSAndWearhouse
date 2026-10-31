namespace POS.Core.Specifications.KitchenSpecs;

public class KitchenTypeSpecs : BaseSpecifications<KitchenType>
{
    public KitchenTypeSpecs()
        => AddInclude();

    public KitchenTypeSpecs(int id ) : base(k => k.Id == id )
       => AddInclude();

    public KitchenTypeSpecs(string kitchenName) : base(k => k.KitchenName == kitchenName)
     => AddInclude();

    private void AddInclude()
     =>   Includes.Add(k => k.KitchenPrinters!);
}
