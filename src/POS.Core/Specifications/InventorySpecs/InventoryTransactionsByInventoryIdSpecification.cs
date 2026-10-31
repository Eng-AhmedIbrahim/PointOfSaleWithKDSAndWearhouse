namespace POS.Core.Specifications.InventorySpecs;

public class InventoryTransactionsByInventoryIdSpecification : BaseSpecifications<InventoryTransaction>
{
    public InventoryTransactionsByInventoryIdSpecification(int inventoryId)
        : base(x => x.InventoryItemId == inventoryId)
    {
        AddOrderByDesc(x => x.CreatedAt);
        Includes.Add(x => x.Images);
    }
}
