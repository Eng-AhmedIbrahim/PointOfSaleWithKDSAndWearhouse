namespace POS.Core.Specifications.AttributeSpecs;

public class AttributeWithIncludeSpecs :BaseSpecifications<Attributes>
{
    
    public AttributeWithIncludeSpecs()
    {
        AddIncludes();
    }

    public AttributeWithIncludeSpecs(int id) :base
        (a => a.Id.Equals(id))
    {
        AddIncludes();
    }
    private void AddIncludes()
    {
        Includes.Add(a => a.AttributeItems);
        IncludeStrings.Add($"{nameof(Attributes.AttributeItems)}.{nameof(AttributeItem.RelatedMenuItem)}");
        Includes.Add(a => a.AttributeGroups);
    }
}
