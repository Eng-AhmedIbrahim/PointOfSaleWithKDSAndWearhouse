using POS.Core.Entities.Item;

namespace POS.Core.Specifications.InventorySpecs;

public class WasteAndDamageTransactionsSpecification : BaseSpecifications<InventoryTransaction>
{
    public WasteAndDamageTransactionsSpecification(DateTime fromDate, DateTime toDate)
        : base(x => (x.Type == TransactionType.Waste || x.Type == TransactionType.Damage) 
                  && x.CreatedAt >= fromDate.Date 
                  && x.CreatedAt < toDate.Date.AddDays(1))
    {
        AddOrderByDesc(x => x.CreatedAt);
        Includes.Add(x => x.InventoryItem!);
        Includes.Add(x => x.Images);
    }
}
