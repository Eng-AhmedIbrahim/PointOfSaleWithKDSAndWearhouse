namespace POS.Core.Specifications.InventorySpecs;

public class InventoryItemByMenuSalesItemIdSpecification : BaseSpecifications<InventoryItem>
{
    public InventoryItemByMenuSalesItemIdSpecification(int itemId)
        : base(x => x.MenuSalesItemId == itemId)
    {
        Includes.Add(x => x.MenuSalesItem!);
    }
}