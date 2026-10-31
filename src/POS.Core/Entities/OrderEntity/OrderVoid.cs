using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.OrderEntity;

public class OrderVoid : BaseEntity
{
    public int OrderId { get; set; }
    public OrderTypes OrderType { get; set; }
    public OrderStates OrderStateAtVoid { get; set; }
    public DateTime VoidDate { get; set; } = DateTime.Now;
    public string VoidedBy { get; set; } = string.Empty;
    public string VoidedByName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool IsFullVoid { get; set; }

    // Totals Before Void
    public decimal SubtotalBefore { get; set; }
    public decimal TaxBefore { get; set; }
    public decimal ServiceBefore { get; set; }
    public decimal DeliveryFeesBefore { get; set; }
    public decimal DiscountBefore { get; set; }
    public decimal GrandTotalBefore { get; set; }

    // Totals After Void
    public decimal SubtotalAfter { get; set; }
    public decimal TaxAfter { get; set; }
    public decimal ServiceAfter { get; set; }
    public decimal DeliveryFeesAfter { get; set; }
    public decimal DiscountAfter { get; set; }
    public decimal GrandTotalAfter { get; set; }

    // The impact of this specific void operation
    public decimal TotalVoidedAmount { get; set; }

    // Navigation properties
    [ForeignKey("OrderId")]
    public Orders? Order { get; set; }

    public ICollection<OrderVoidItem> VoidItems { get; set; } = new List<OrderVoidItem>();
}
