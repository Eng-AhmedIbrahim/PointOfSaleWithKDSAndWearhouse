using POS.Contract.Dtos.ReportingDtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using POS.Core.Repository.Contract;
using AutoMapper;
using Pos.Repository.Data;
using POS.Core.Entities.Payment;
using POS.Core.Entities.Item;
using POS.Core.Entities.Warehouse;

namespace POS.Services.ReportingServices;

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetExpensesAsync(DateTime from, DateTime to);
    Task<ExpenseDto> AddExpenseAsync(ExpenseDto expense);
    Task<bool> DeleteExpenseAsync(int id);
}

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ExpenseService(AppDbContext context, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(DateTime from, DateTime to)
    {
        var nextDay = to.Date.AddDays(1);
        var expenses = await _context.Expenses
            .Include(e => e.Items)
            .Where(e => e.Date >= from.Date && e.Date < nextDay)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        return _mapper.Map<List<ExpenseDto>>(expenses);
    }

    public async Task<ExpenseDto> AddExpenseAsync(ExpenseDto expenseDto)
    {
        // Auto-calculate total amount from line items if provided
        if (expenseDto.Items != null && expenseDto.Items.Any())
        {
            var calculatedTotal = expenseDto.Items.Sum(i => i.TotalPrice > 0 ? i.TotalPrice : (i.Quantity * i.UnitPrice));
            if (calculatedTotal > 0)
            {
                expenseDto.Amount = calculatedTotal;
            }
        }

        var expense = new Expense
        {
            Date = expenseDto.Date == default ? DateTime.Now : expenseDto.Date,
            Amount = expenseDto.Amount,
            Description = expenseDto.Description ?? string.Empty,
            Category = expenseDto.Category,
            CreatedById = expenseDto.CreatedById,
            CreatedByName = expenseDto.CreatedByName,
            SpentBy = expenseDto.SpentBy,
            IsPayoutFromDrawer = expenseDto.IsPayoutFromDrawer,
            BranchId = expenseDto.BranchId,
            ShiftId = expenseDto.ShiftId
        };

        if (expenseDto.Items != null && expenseDto.Items.Any())
        {
            foreach (var itemDto in expenseDto.Items)
            {
                var item = new ExpenseItem
                {
                    ItemType = (ExpenseItemType)itemDto.ItemType,
                    ItemId = itemDto.ItemId,
                    ItemName = itemDto.ItemName ?? string.Empty,
                    Quantity = itemDto.Quantity > 0 ? itemDto.Quantity : 1,
                    UnitPrice = itemDto.UnitPrice,
                    TotalPrice = itemDto.TotalPrice > 0 ? itemDto.TotalPrice : (itemDto.Quantity * itemDto.UnitPrice),
                    UnitName = itemDto.UnitName,
                    Notes = itemDto.Notes
                };
                expense.Items.Add(item);
            }
        }

        await _context.Expenses.AddAsync(expense);
        await _context.SaveChangesAsync();

        expenseDto.Id = expense.Id;

        // Process stock updates and stock movements for raw materials and sales items
        int branchId = expense.BranchId ?? 1;
        if (expense.Items != null && expense.Items.Any())
        {
            foreach (var item in expense.Items)
            {
                if (item.Quantity <= 0) continue;

                if (item.ItemType == ExpenseItemType.SalesItem && item.ItemId.HasValue)
                {
                    await UpdateSalesItemInventoryStock(item.ItemId.Value, item.Quantity, item.UnitPrice, expense, item.ItemName);
                }
                else if (item.ItemType == ExpenseItemType.RawMaterial && item.ItemId.HasValue)
                {
                    await UpdateRawMaterialWarehouseStock(item.ItemId.Value, item.Quantity, item.UnitPrice, expense, item.ItemName, branchId);
                }
            }
        }

        return _mapper.Map<ExpenseDto>(expense);
    }

    private async Task UpdateSalesItemInventoryStock(int menuSalesItemId, decimal quantity, decimal unitPrice, Expense expense, string itemName)
    {
        try
        {
            int branchId = expense.BranchId ?? 1;
            var inventory = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.MenuSalesItemId == menuSalesItemId && (i.BranchId == branchId || i.BranchId == null));

            if (inventory == null)
            {
                var menuItem = await _context.MenuSalesItems.FindAsync(menuSalesItemId);
                inventory = new InventoryItem
                {
                    MenuSalesItemId = menuSalesItemId,
                    BranchId = branchId,
                    CurrentQuantity = quantity,
                    MinimumQuantity = 0,
                    TrackInventory = true,
                    UnitCost = unitPrice,
                    ItemNameAr = menuItem?.ArabicName ?? itemName,
                    ItemNameEn = menuItem?.EnglishName,
                    CategoryId = menuItem?.CategoryId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.InventoryItems.Add(inventory);
                await _context.SaveChangesAsync();
            }
            else
            {
                inventory.CurrentQuantity += quantity;
                if (unitPrice > 0) inventory.UnitCost = unitPrice;
                inventory.UpdatedAt = DateTime.UtcNow;
                _context.InventoryItems.Update(inventory);
            }

            var trans = new InventoryTransaction
            {
                InventoryItemId = inventory.Id,
                QuantityChange = quantity,
                ResultingQuantity = inventory.CurrentQuantity,
                UnitCost = unitPrice,
                Type = TransactionType.StockIn,
                ReferenceId = $"EXP-{expense.Id}",
                Notes = $"شراء نقدي من الدرج (مصروف) - {expense.Description}".Trim(),
                Reason = "مشتريات فرع نقدية",
                CreatedBy = expense.CreatedByName ?? "System",
                CreatedAt = DateTime.UtcNow
            };
            _context.InventoryTransactions.Add(trans);

            // Record Cost Batch for Branch Inventory (Priority 1 = Expense purchase is consumed FIRST!)
            _context.BranchInventoryCostBatches.Add(new POS.Core.Entities.Inventory.BranchInventoryCostBatch
            {
                BranchId = branchId,
                MenuSalesItemId = menuSalesItemId,
                ItemName = itemName,
                BatchSource = "ExpensePurchase",
                ReferenceId = $"EXP-{expense.Id}",
                OriginalQuantity = quantity,
                RemainingQuantity = quantity,
                UnitCost = unitPrice,
                Priority = 1,
                ReceivedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseStock] Error updating sales item stock: {ex.Message}");
        }
    }

    private async Task UpdateRawMaterialWarehouseStock(int warehouseProductId, decimal quantity, decimal unitPrice, Expense expense, string itemName, int branchId)
    {
        try
        {
            var product = await _context.WarehouseProducts.FindAsync(warehouseProductId);
            if (product == null)
            {
                var menuItem = await _context.MenuSalesItems.FindAsync(warehouseProductId);
                if (menuItem != null)
                {
                    await UpdateSalesItemInventoryStock(menuItem.Id, quantity, unitPrice, expense, itemName);
                }
                return;
            }

            // 1. Warehouse Module: Update StockBalances & StockLedger
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.BranchId == branchId && w.IsActive)
                ?? await _context.Warehouses.FirstOrDefaultAsync(w => w.IsActive);

            if (warehouse != null)
            {
                var balance = await _context.StockBalances
                    .FirstOrDefaultAsync(b => b.WarehouseId == warehouse.Id && b.ProductId == warehouseProductId);

                if (balance == null)
                {
                    balance = new StockBalance
                    {
                        WarehouseId = warehouse.Id,
                        ProductId = warehouseProductId,
                        QuantityOnHand = quantity,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.StockBalances.Add(balance);
                }
                else
                {
                    balance.QuantityOnHand += quantity;
                    balance.LastUpdated = DateTime.UtcNow;
                }

                decimal cost = unitPrice > 0 ? unitPrice : (product.WeightedAverageCost > 0 ? product.WeightedAverageCost : product.LastPurchaseCost);
                var now = DateTime.UtcNow;

                var ledger = new StockLedger
                {
                    WarehouseId = warehouse.Id,
                    ProductId = warehouseProductId,
                    MovementType = StockMovementType.GoodsReceipt,
                    QuantityIn = quantity,
                    QuantityOut = 0,
                    RunningBalance = balance.QuantityOnHand,
                    UnitCost = cost,
                    TotalCost = quantity * cost,
                    ReferenceDocumentType = "إذن صرف نقدية",
                    ReferenceDocumentNumber = $"EXP-{expense.Id:D5}",
                    Notes = $"شراء مادة خام من الدرج: {itemName} ({expense.Description})".Trim(),
                    MovementDate = now,
                    CreatedByUserId = expense.CreatedById
                };
                _context.StockLedger.Add(ledger);
                await _context.SaveChangesAsync();
            }

            // 2. Branch Inventory: Also update branch InventoryItems if raw material exists there
            var rawMenuItem = await _context.MenuSalesItems
                .FirstOrDefaultAsync(m => m.ArabicName == product.NameAr || (m.Barcode != null && m.Barcode == product.Barcode));
            if (rawMenuItem != null)
            {
                await UpdateSalesItemInventoryStock(rawMenuItem.Id, quantity, unitPrice, expense, product.NameAr);

                // Also add cost batch for raw material (Priority 1 = expense, consumed first)
                _context.BranchInventoryCostBatches.Add(new POS.Core.Entities.Inventory.BranchInventoryCostBatch
                {
                    BranchId = branchId,
                    MenuSalesItemId = rawMenuItem.Id,
                    ItemName = product.NameAr,
                    BatchSource = "ExpensePurchase",
                    ReferenceId = $"EXP-{expense.Id}",
                    OriginalQuantity = quantity,
                    RemainingQuantity = quantity,
                    UnitCost = unitPrice,
                    Priority = 1,
                    ReceivedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ExpenseStock] Error updating raw material warehouse stock: {ex.Message}");
        }
    }

    public async Task<bool> DeleteExpenseAsync(int id)
    {
        var expense = await _context.Expenses.Include(e => e.Items).FirstOrDefaultAsync(e => e.Id == id);
        if (expense == null) return false;

        _context.Expenses.Remove(expense);
        return await _context.SaveChangesAsync() > 0;
    }
}
