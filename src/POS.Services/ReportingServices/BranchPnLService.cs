using Microsoft.EntityFrameworkCore;
using POS.Core.Entities.Company;
using POS.Core.Entities.Inventory;
using POS.Core.Entities.Item;
using POS.Core.Entities.OrderEntity;
using POS.Core.Entities.Payment;
using POS.Core.Entities.Reporting;
using Pos.Repository.Data;
using Serilog;

namespace POS.Services.ReportingServices;

public interface IBranchPnLService
{
    Task<BranchDailyPnL?> CalculateAndSaveAsync(DateTime date, int branchId = 1);
}

public class BranchPnLService : IBranchPnLService
{
    private readonly AppDbContext _context;

    public BranchPnLService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BranchDailyPnL?> CalculateAndSaveAsync(DateTime date, int branchId = 1)
    {
        try
        {
            // 1. Check if feature is enabled (read from DB directly)
            var featureSetting = await _context.Set<POS.Core.Entities.OrderEntity.PosFeatureSetting>()
                .FirstOrDefaultAsync(s => s.FeatureName == "EnableBranchCostAndPnlTracking");

            if (featureSetting == null || !featureSetting.Value)
            {
                Log.Information("BranchPnLService: Feature disabled. Skipping P&L calc for {Date}", date.Date);
                return null;
            }

            var dateOnly = date.Date;
            var nextDate = dateOnly.AddDays(1);

            // 2. Load all completed / finalized orders for the day
            var orders = await _context.Set<Orders>()
                .Include(o => o.OrderDetails)
                .Where(o =>
                    o.OrderDate.HasValue &&
                    o.OrderDate.Value.Date == dateOnly &&
                    o.BranchID == branchId &&
                    (o.OrderState == OrderStates.Completed ||
                     o.OrderState == OrderStates.Delivered ||
                     o.OrderState == OrderStates.Dispatched))
                .ToListAsync();

            // Net sales is the actual revenue after discount
            decimal netSales = orders.Sum(o => o.GrandTotal ?? 0m);
            decimal totalDiscounts = orders.Sum(o => (o.TotalDiscount ?? 0) > 0 ? o.TotalDiscount.Value : (o.Discount ?? 0m));
            decimal totalVoids = orders.Sum(o => (o.TotalVoid ?? 0) > 0 ? o.TotalVoid.Value : (o.VoidAmount ?? 0m));
            decimal totalGrossSales = netSales + totalDiscounts;
            int orderCount = orders.Count;

            // 3. Load all active recipes
            var allRecipes = await _context.Set<Recipe>()
                .Include(r => r.Ingredients)
                .Where(r => r.IsActive)
                .ToListAsync();

            var recipeMap = allRecipes.ToDictionary(r => r.MenuSalesItemId);

            // 4. Load fallback UnitCost from InventoryItem (for items without cost batches)
            var inventoryItems = await _context.Set<InventoryItem>()
                .Where(i => i.BranchId == null || i.BranchId == branchId)
                .ToListAsync();

            var fallbackCostMap = inventoryItems
                .Where(i => i.UnitCost > 0)
                .GroupBy(i => i.MenuSalesItemId)
                .ToDictionary(g => g.Key, g => g.First().UnitCost);

            // 5. Load active cost batches for this branch (tracked, ordered by Priority ASC, ReceivedAt ASC)
            var costBatches = await _context.Set<BranchInventoryCostBatch>()
                .Where(b => b.BranchId == branchId && !b.IsExhausted && b.RemainingQuantity > 0)
                .OrderBy(b => b.Priority)
                .ThenBy(b => b.ReceivedAt)
                .ToListAsync();

            var batchLookup = costBatches
                .GroupBy(b => b.MenuSalesItemId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(b => b.Priority).ThenBy(b => b.ReceivedAt).ToList());

            // 6. Calculate COGS & Deplete Batches in FIFO/Priority Order
            decimal totalCOGS = 0m;

            foreach (var order in orders)
            {
                if (order.OrderDetails == null) continue;

                foreach (var detail in order.OrderDetails)
                {
                    if (detail.MenuSalesItemId == null) continue;

                    // Skip fully voided items (customer didn't receive them)
                    if (detail.IsVoided == true) continue;

                    int soldItemId = detail.MenuSalesItemId.Value;

                    // Deduct partial void quantity (VoidAmount in OrderItemsDetails represents voided quantity)
                    decimal voidQty = detail.VoidAmount ?? 0m;
                    decimal soldQty = Math.Max(0m, (detail.Quantity ?? 1m) - voidQty);

                    if (soldQty <= 0) continue;

                    if (recipeMap.TryGetValue(soldItemId, out var recipe))
                    {
                        // Recipe-based manufactured item: consume ingredients
                        foreach (var ingredient in recipe.Ingredients)
                        {
                            decimal ingredientQtyNeeded = ingredient.Quantity * soldQty;
                            decimal ingredientCost = ConsumeFromBatches(batchLookup, fallbackCostMap, ingredient.MenuSalesIngredientId, ingredientQtyNeeded);
                            totalCOGS += ingredientCost;
                        }
                    }
                    else
                    {
                        // Direct retail item: consume directly
                        decimal itemCost = ConsumeFromBatches(batchLookup, fallbackCostMap, soldItemId, soldQty);
                        totalCOGS += itemCost;
                    }
                }
            }

            // 7. Calculate Waste Cost (الهالك والتالف من الفويد بدون إرجاع أو تسجيل هالك مباشر)
            var wasteTransactions = await _context.Set<InventoryTransaction>()
                .Include(t => t.InventoryItem)
                .Where(t =>
                    (t.Type == TransactionType.Waste || t.Type == TransactionType.Damage) &&
                    t.CreatedAt >= dateOnly && t.CreatedAt < nextDate)
                .ToListAsync();

            decimal totalWasteCost = 0m;
            foreach (var wt in wasteTransactions)
            {
                decimal qty = Math.Abs(wt.QuantityChange);
                decimal unitCost = wt.UnitCost;

                if (unitCost <= 0 && wt.InventoryItem != null)
                {
                    unitCost = wt.InventoryItem.UnitCost;
                    if (unitCost <= 0 && fallbackCostMap.TryGetValue(wt.InventoryItem.MenuSalesItemId, out var fc))
                    {
                        unitCost = fc;
                    }
                }

                totalWasteCost += qty * unitCost;
            }

            // 8. Load expenses for the day and properly separate Operating Expenses from Inventory Purchases
            var allExpenses = await _context.Set<Expense>()
                .Include(e => e.Items)
                .Where(e =>
                    e.BranchId == branchId &&
                    e.Date >= dateOnly && e.Date < nextDate)
                .ToListAsync();

            decimal totalOperatingExpenses = 0m;
            decimal totalInventoryPurchases = 0m;

            foreach (var exp in allExpenses)
            {
                string cat = (exp.Category ?? string.Empty).Trim();

                bool isInventoryPurchase =
                    cat == "مشتريات خامات" ||
                    cat == "مشتريات بضاعة للبيع" ||
                    cat == "RawMaterialPurchase" ||
                    cat == "SalesItemPurchase" ||
                    exp.Items.Any(i => i.ItemType == ExpenseItemType.RawMaterial || i.ItemType == ExpenseItemType.SalesItem);

                if (isInventoryPurchase)
                {
                    totalInventoryPurchases += exp.Amount;
                }
                else
                {
                    totalOperatingExpenses += exp.Amount;
                }
            }

            // 9. P&L Financial Calculations
            decimal grossProfit        = netSales - totalCOGS;
            decimal grossProfitMargin  = netSales > 0 ? Math.Round(grossProfit / netSales * 100, 2) : 0m;
            // Net profit = GrossProfit - Operating Expenses - Waste/Damage Loss
            decimal netProfit          = grossProfit - totalOperatingExpenses - totalWasteCost;

            // 10. Branch Name
            var branch = await _context.Set<Branch>()
                .FirstOrDefaultAsync(b => b.Id == branchId);

            // 11. Upsert snapshot
            var existingSnapshot = await _context.Set<BranchDailyPnL>()
                .FirstOrDefaultAsync(s => s.Date == dateOnly && s.BranchId == branchId);

            if (existingSnapshot != null)
                _context.Set<BranchDailyPnL>().Remove(existingSnapshot);

            var snapshot = new BranchDailyPnL
            {
                Date                    = dateOnly,
                BranchId                = branchId,
                BranchName              = branch?.Name ?? "الفرع الرئيسي",
                TotalGrossSales         = totalGrossSales,
                TotalDiscounts          = totalDiscounts,
                TotalVoids              = totalVoids,
                NetSales                = netSales,
                TotalCOGS               = totalCOGS,
                TotalWasteCost          = totalWasteCost,
                TotalInventoryPurchases = totalInventoryPurchases,
                GrossProfit             = grossProfit,
                GrossProfitMargin       = grossProfitMargin,
                TotalExpenses           = totalOperatingExpenses,
                NetProfit               = netProfit,
                OrderCount              = orderCount,
                CalculatedAt            = DateTime.UtcNow
            };

            await _context.Set<BranchDailyPnL>().AddAsync(snapshot);

            // Save both depleted batches and new snapshot to database
            await _context.SaveChangesAsync();

            Log.Information(
                "BranchPnLService ✅ Date={Date} NetSales={NS:0.##} COGS={COGS:0.##} Waste={W:0.##} GP={GP:0.##} Exp={Exp:0.##} NP={NP:0.##}",
                dateOnly, netSales, totalCOGS, totalWasteCost, grossProfit, totalOperatingExpenses, netProfit);

            return snapshot;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BranchPnLService: Failed to calculate P&L for date {Date}", date);
            return null;
        }
    }

    /// <summary>
    /// Consumes the required quantity from active cost batches in Priority order (1=Drawer Expense first, 2=Warehouse Transfer after).
    /// Depletes RemainingQuantity and marks IsExhausted when batch reaches 0.
    /// Falls back to InventoryItem.UnitCost if batches are exhausted or not found.
    /// </summary>
    private static decimal ConsumeFromBatches(
        Dictionary<int, List<BranchInventoryCostBatch>> batchLookup,
        Dictionary<int, decimal> fallbackCostMap,
        int menuSalesItemId,
        decimal qtyNeeded)
    {
        if (qtyNeeded <= 0) return 0m;

        decimal totalCost = 0m;
        decimal remaining = qtyNeeded;

        if (batchLookup.TryGetValue(menuSalesItemId, out var batches))
        {
            foreach (var batch in batches)
            {
                if (remaining <= 0) break;
                if (batch.IsExhausted || batch.RemainingQuantity <= 0) continue;

                decimal take = Math.Min(remaining, batch.RemainingQuantity);
                totalCost += take * batch.UnitCost;
                batch.RemainingQuantity -= take;

                if (batch.RemainingQuantity <= 0.0001m)
                {
                    batch.RemainingQuantity = 0;
                    batch.IsExhausted = true;
                }

                remaining -= take;
            }
        }

        // If quantity still remains (either no batches ever existed or all batches exhausted),
        // fallback to standard configured UnitCost so COGS is never falsely zero
        if (remaining > 0)
        {
            decimal fallbackUnitCost = 0m;
            if (fallbackCostMap.TryGetValue(menuSalesItemId, out var fc) && fc > 0)
            {
                fallbackUnitCost = fc;
            }
            else if (batches != null && batches.Any())
            {
                fallbackUnitCost = batches.Last().UnitCost;
            }

            totalCost += remaining * fallbackUnitCost;
        }

        return totalCost;
    }
}
