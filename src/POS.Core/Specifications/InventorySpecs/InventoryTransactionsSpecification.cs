using POS.Core.Entities.Item;
using System;

namespace POS.Core.Specifications.InventorySpecs;

public class InventoryTransactionsSpecification : BaseSpecifications<InventoryTransaction>
{
    public InventoryTransactionsSpecification(DateTime? fromDate = null, DateTime? toDate = null, int? inventoryId = null)
        : base(x => 
            (!fromDate.HasValue || x.CreatedAt >= fromDate.Value.Date) &&
            (!toDate.HasValue || x.CreatedAt < toDate.Value.Date.AddDays(1)) &&
            (!inventoryId.HasValue || x.InventoryItemId == inventoryId.Value))
    {
        AddOrderByDesc(x => x.CreatedAt);
        Includes.Add(x => x.InventoryItem);
        Includes.Add(x => x.Images);
    }
}
