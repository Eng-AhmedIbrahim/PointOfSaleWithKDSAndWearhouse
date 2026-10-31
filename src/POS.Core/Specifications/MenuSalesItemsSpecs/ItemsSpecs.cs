namespace POS.Core.Specifications.MenuSalesItemsSpecs;

public class ItemsSpecs : BaseSpecifications<MenuSalesItems>
{
    public ItemsSpecs(Expression<Func<MenuSalesItems, bool>> criteria) : base(criteria)
    {
        AddInclude();
    }

    private void AddInclude()
    {
        Includes.Add(s => s.Attribute!);
        IncludeStrings.Add($"{nameof(MenuSalesItems.Attribute)}.{nameof(Attributes.AttributeItems)}.{nameof(AttributeItem.RelatedMenuItem)}");
    }
    public int? ItemId { get; set; }
}