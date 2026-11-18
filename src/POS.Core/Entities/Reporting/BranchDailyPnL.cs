using System;

namespace POS.Core.Entities.Reporting;

/// <summary>
/// Pre-calculated daily Income Statement (P&L) snapshot for a branch.
/// Stored cleanly per day to be synced directly to HQ without re-processing millions of line items.
/// </summary>
public class BranchDailyPnL : BaseEntity
{
    public DateTime Date { get; set; }
    public int BranchId { get; set; } = 1;
    public string? BranchName { get; set; }

    public decimal TotalGrossSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalVoids { get; set; }
    public decimal NetSales { get; set; }

    /// <summary>Cost of Goods Sold (raw materials consumed via recipe + retail sales items cost)</summary>
    public decimal TotalCOGS { get; set; }

    /// <summary>Cost of spoiled, damaged, or wasted raw materials/items (e.g. food prepared then voided without restock)</summary>
    public decimal TotalWasteCost { get; set; }

    /// <summary>Cash payouts from drawer used specifically to buy inventory/raw materials (asset purchase, not operating expense)</summary>
    public decimal TotalInventoryPurchases { get; set; }

    /// <summary>Gross Profit = NetSales - TotalCOGS</summary>
    public decimal GrossProfit { get; set; }

    /// <summary>Gross Profit %</summary>
    public decimal GrossProfitMargin { get; set; }

    /// <summary>Cash payouts / expenses recorded from drawer</summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>Net Operating Profit = GrossProfit - TotalExpenses</summary>
    public decimal NetProfit { get; set; }

    public int OrderCount { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
