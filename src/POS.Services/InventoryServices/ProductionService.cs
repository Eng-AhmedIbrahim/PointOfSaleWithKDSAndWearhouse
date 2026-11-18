using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Contract.Dtos.InventoryDtos;
using POS.Core.Entities;
using POS.Core.Entities.Warehouse;
using POS.Core.Services.Contract.InventoryServices;
using System.Data;

namespace POS.Services.InventoryServices;

public class ProductionService : IProductionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductionService> _logger;

    public ProductionService(AppDbContext context, ILogger<ProductionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ProductionOrderDto>> GetProductionOrdersAsync(int? warehouseId = null)
    {
        var query = _context.ProductionOrders
            .Include(o => o.Warehouse)
            .Include(o => o.ProducedProduct)
                .ThenInclude(p => p.BaseUnit)
            .Include(o => o.ProductionRecipe)
            .Include(o => o.Lines)
                .ThenInclude(l => l.IngredientProduct)
            .AsNoTracking();

        if (warehouseId.HasValue && warehouseId.Value > 0)
        {
            query = query.Where(o => o.WarehouseId == warehouseId.Value);
        }

        var list = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

        return list.Select(o => MapToDto(o)).ToList();
    }

    public async Task<ProductionOrderDto?> GetProductionOrderByIdAsync(int id)
    {
        var order = await _context.ProductionOrders
            .Include(o => o.Warehouse)
            .Include(o => o.ProducedProduct)
                .ThenInclude(p => p.BaseUnit)
            .Include(o => o.ProductionRecipe)
            .Include(o => o.Lines)
                .ThenInclude(l => l.IngredientProduct)
                    .ThenInclude(ip => ip.BaseUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        return order == null ? null : MapToDto(order);
    }

    public async Task<List<ProductionRecipeIngredientDto>> CalculateRequiredIngredientsAsync(int warehouseId, int productId, decimal targetQty)
    {
        var recipe = await _context.ProductionRecipes
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.IngredientProduct)
                    .ThenInclude(ip => ip.BaseUnit)
            .FirstOrDefaultAsync(r => r.ProducedProductId == productId && r.IsActive);

        if (recipe == null || !recipe.Ingredients.Any())
            return new List<ProductionRecipeIngredientDto>();

        decimal yieldFactor = recipe.YieldQuantity > 0 ? (targetQty / recipe.YieldQuantity) : targetQty;

        var productIds = recipe.Ingredients.Select(i => i.IngredientProductId).ToList();
        var stockBalances = await _context.StockBalances
            .Where(b => b.WarehouseId == warehouseId && productIds.Contains(b.ProductId))
            .ToDictionaryAsync(b => b.ProductId, b => b.QuantityOnHand);

        var result = new List<ProductionRecipeIngredientDto>();

        foreach (var ing in recipe.Ingredients)
        {
            decimal baseQty = ing.Quantity * yieldFactor;
            decimal wasteQty = ing.WastePercent > 0 ? baseQty * (ing.WastePercent / 100m) : 0;
            decimal totalRequired = Math.Round(baseQty + wasteQty, 3);

            stockBalances.TryGetValue(ing.IngredientProductId, out decimal available);

            decimal cost = ing.IngredientProduct != null 
                ? (ing.IngredientProduct.LastPurchaseCost > 0 ? ing.IngredientProduct.LastPurchaseCost : ing.IngredientProduct.WeightedAverageCost)
                : 0;

            result.Add(new ProductionRecipeIngredientDto
            {
                Id = ing.Id,
                ProductionRecipeId = ing.ProductionRecipeId,
                IngredientProductId = ing.IngredientProductId,
                IngredientProductName = ing.IngredientProduct?.NameAr ?? $"خامة {ing.IngredientProductId}",
                IngredientProductSku = ing.IngredientProduct?.SKU,
                Quantity = totalRequired,
                UnitId = ing.UnitId ?? ing.IngredientProduct?.BaseUnitId,
                UnitName = ing.IngredientProduct?.BaseUnit?.NameAr ?? "وحدة",
                WastePercent = ing.WastePercent,
                EstimatedCost = Math.Round(totalRequired * cost, 2),
                CurrentStockAvailable = available,
                Notes = ing.Notes
            });
        }

        return result;
    }

    public async Task<ProductionOrderDto> CreateProductionOrderAsync(CreateProductionOrderRequest request)
    {
        var count = await _context.ProductionOrders.CountAsync(o => o.OrderDate.Date == DateTime.UtcNow.Date);
        string orderNum = $"PRD-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D3}";

        var producedProduct = await _context.WarehouseProducts
            .Include(p => p.BaseUnit)
            .FirstOrDefaultAsync(p => p.Id == request.ProducedProductId);

        if (producedProduct == null)
            throw new Exception("الصنف المراد تصنيعه غير موجود");

        var order = new ProductionOrder
        {
            OrderNumber = orderNum,
            WarehouseId = request.WarehouseId,
            ProducedProductId = request.ProducedProductId,
            ProductionRecipeId = request.ProductionRecipeId,
            TargetQuantity = request.TargetQuantity,
            ActualQuantityProduced = request.ActualQuantityProduced,
            Status = ProductionOrderStatus.Planned,
            OrderDate = DateTime.UtcNow,
            Notes = request.Notes,
            CreatedByUserName = request.CreatedByUserName
        };

        // Populate lines: if lines were provided use them, otherwise calculate from recipe
        if (request.Lines != null && request.Lines.Any())
        {
            foreach (var line in request.Lines)
            {
                order.Lines.Add(new ProductionOrderLine
                {
                    IngredientProductId = line.IngredientProductId,
                    PlannedQuantity = line.PlannedQuantity,
                    ActualQuantityConsumed = line.ActualQuantityConsumed > 0 ? line.ActualQuantityConsumed : line.PlannedQuantity,
                    UnitCost = line.UnitCost,
                    TotalCost = (line.ActualQuantityConsumed > 0 ? line.ActualQuantityConsumed : line.PlannedQuantity) * line.UnitCost,
                    AvailableStockAtExecution = line.AvailableStockAtExecution,
                    Notes = line.Notes
                });
            }
        }
        else
        {
            var calculated = await CalculateRequiredIngredientsAsync(request.WarehouseId, request.ProducedProductId, request.TargetQuantity);
            foreach (var ing in calculated)
            {
                decimal unitCost = ing.Quantity > 0 ? (ing.EstimatedCost / ing.Quantity) : 0;
                order.Lines.Add(new ProductionOrderLine
                {
                    IngredientProductId = ing.IngredientProductId,
                    PlannedQuantity = ing.Quantity,
                    ActualQuantityConsumed = ing.Quantity,
                    UnitCost = unitCost,
                    TotalCost = ing.EstimatedCost,
                    AvailableStockAtExecution = ing.CurrentStockAvailable,
                    Notes = ing.Notes
                });
            }
        }

        decimal totalCost = order.Lines.Sum(l => l.TotalCost);
        order.TotalActualCost = totalCost;
        order.UnitCost = order.ActualQuantityProduced > 0 ? Math.Round(totalCost / order.ActualQuantityProduced, 2) : 0;

        _context.ProductionOrders.Add(order);
        await _context.SaveChangesAsync();

        // If requested to execute immediately (standard commissary run)
        if (request.AutoExecuteImmediately)
        {
            await ExecuteProductionRunInternalAsync(order);
        }

        return (await GetProductionOrderByIdAsync(order.Id))!;
    }

    public async Task<bool> CompleteProductionOrderAsync(int id, decimal actualProducedQty, string? userId = null)
    {
        var order = await _context.ProductionOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null || order.Status == ProductionOrderStatus.Completed || order.Status == ProductionOrderStatus.Cancelled)
            return false;

        if (actualProducedQty > 0)
            order.ActualQuantityProduced = actualProducedQty;

        await ExecuteProductionRunInternalAsync(order);
        return true;
    }

    private async Task ExecuteProductionRunInternalAsync(ProductionOrder order)
    {
        var producedProduct = await _context.WarehouseProducts.FindAsync(order.ProducedProductId);
        if (producedProduct == null) return;

        // 1. Consume Ingredients from Stock
        decimal actualTotalCost = 0;

        foreach (var line in order.Lines)
        {
            decimal qtyToConsume = line.ActualQuantityConsumed > 0 ? line.ActualQuantityConsumed : line.PlannedQuantity;
            if (qtyToConsume <= 0) continue;

            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == order.WarehouseId && b.ProductId == line.IngredientProductId);

            if (balance == null)
            {
                balance = new StockBalance
                {
                    WarehouseId = order.WarehouseId,
                    ProductId = line.IngredientProductId,
                    QuantityOnHand = 0
                };
                _context.StockBalances.Add(balance);
            }

            balance.QuantityOnHand -= qtyToConsume;

            var ingredientProduct = await _context.WarehouseProducts.FindAsync(line.IngredientProductId);
            decimal costPerUnit = line.UnitCost > 0 ? line.UnitCost : (ingredientProduct?.LastPurchaseCost > 0 ? ingredientProduct.LastPurchaseCost : (ingredientProduct?.WeightedAverageCost ?? 0));
            decimal lineTotalCost = Math.Round(qtyToConsume * costPerUnit, 2);
            line.UnitCost = costPerUnit;
            line.TotalCost = lineTotalCost;
            actualTotalCost += lineTotalCost;

            // Record Ledger Movement (Production Consume)
            _context.StockLedger.Add(new StockLedger
            {
                WarehouseId = order.WarehouseId,
                ProductId = line.IngredientProductId,
                MovementType = StockMovementType.ProductionConsume,
                QuantityIn = 0,
                QuantityOut = qtyToConsume,
                RunningBalance = balance.QuantityOnHand,
                UnitCost = costPerUnit,
                TotalCost = lineTotalCost,
                ReferenceDocumentType = "أمر تصنيع وتشغيل مركزي",
                ReferenceDocumentNumber = order.OrderNumber,
                MovementDate = DateTime.UtcNow,
                Notes = $"صرف خامات لتشغيل أمر إنتاج {order.OrderNumber} ({producedProduct.NameAr})"
            });
        }

        // 2. Add Output Manufactured Product to Stock
        var producedBalance = await _context.StockBalances
            .FirstOrDefaultAsync(b => b.WarehouseId == order.WarehouseId && b.ProductId == order.ProducedProductId);

        if (producedBalance == null)
        {
            producedBalance = new StockBalance
            {
                WarehouseId = order.WarehouseId,
                ProductId = order.ProducedProductId,
                QuantityOnHand = 0
            };
            _context.StockBalances.Add(producedBalance);
        }

        producedBalance.QuantityOnHand += order.ActualQuantityProduced;

        decimal unitOutputCost = order.ActualQuantityProduced > 0 ? Math.Round(actualTotalCost / order.ActualQuantityProduced, 2) : 0;
        order.TotalActualCost = actualTotalCost;
        order.UnitCost = unitOutputCost;
        order.Status = ProductionOrderStatus.Completed;
        order.CompletedDate = DateTime.UtcNow;

        // Update Product Costing in Master Card
        if (unitOutputCost > 0)
        {
            producedProduct.LastPurchaseCost = unitOutputCost;
            if (producedProduct.WeightedAverageCost <= 0)
                producedProduct.WeightedAverageCost = unitOutputCost;
        }

        // Record Ledger Movement (Production Output)
        _context.StockLedger.Add(new StockLedger
        {
            WarehouseId = order.WarehouseId,
            ProductId = order.ProducedProductId,
            MovementType = StockMovementType.ProductionOutput,
            QuantityIn = order.ActualQuantityProduced,
            QuantityOut = 0,
            RunningBalance = producedBalance.QuantityOnHand,
            UnitCost = unitOutputCost,
            TotalCost = actualTotalCost,
            ReferenceDocumentType = "أمر تصنيع وتشغيل مركزي",
            ReferenceDocumentNumber = order.OrderNumber,
            MovementDate = DateTime.UtcNow,
            Notes = $"استلام منتج مصنّع من أمر تشغيل {order.OrderNumber}"
        });

        // 3. Mirror into local Branch Inventory if linked to MenuSalesItems
        try
        {
            var menuItem = await _context.MenuSalesItems
                .FirstOrDefaultAsync(m => m.Barcode == producedProduct.Barcode || m.ArabicName == producedProduct.NameAr);

            if (menuItem != null)
            {
                var branchId = _context.Warehouses.Where(w => w.Id == order.WarehouseId).Select(w => w.BranchId).FirstOrDefault();
                if (branchId.HasValue && branchId.Value > 0)
                {
                    var invItem = await _context.InventoryItems
                        .FirstOrDefaultAsync(i => i.MenuSalesItemId == menuItem.Id && i.BranchId == branchId.Value);

                    if (invItem != null)
                    {
                        invItem.CurrentQuantity += order.ActualQuantityProduced;

                        _context.InventoryTransactions.Add(new InventoryTransaction
                        {
                            InventoryItemId = invItem.Id,
                            QuantityChange = order.ActualQuantityProduced,
                            ResultingQuantity = invItem.CurrentQuantity,
                            Type = TransactionType.StockIn,
                            CreatedAt = DateTime.UtcNow,
                            Reason = "تصنيع وتشغيل مركزي",
                            Notes = $"إنتاج مركزي أمر {order.OrderNumber}",
                            ReferenceId = order.OrderNumber
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mirror production output to branch InventoryItems");
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> CancelProductionOrderAsync(int id, string? reason = null)
    {
        var order = await _context.ProductionOrders.FindAsync(id);
        if (order == null || order.Status == ProductionOrderStatus.Completed)
            return false;

        order.Status = ProductionOrderStatus.Cancelled;
        order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? $"ملغي: {reason}" : $"{order.Notes} | ملغي: {reason}";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ProductionRecipeDto>> GetRecipesAsync()
    {
        var list = await _context.ProductionRecipes
            .Include(r => r.ProducedProduct)
                .ThenInclude(p => p.BaseUnit)
            .Include(r => r.YieldUnit)
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.IngredientProduct)
                    .ThenInclude(ip => ip.BaseUnit)
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.Unit)
            .AsNoTracking()
            .ToListAsync();

        return list.Select(r => MapRecipeToDto(r)).ToList();
    }

    public async Task<ProductionRecipeDto?> GetRecipeByProductIdAsync(int productId)
    {
        var recipe = await _context.ProductionRecipes
            .Include(r => r.ProducedProduct)
                .ThenInclude(p => p.BaseUnit)
            .Include(r => r.YieldUnit)
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.IngredientProduct)
                    .ThenInclude(ip => ip.BaseUnit)
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.Unit)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ProducedProductId == productId && r.IsActive);

        return recipe == null ? null : MapRecipeToDto(recipe);
    }

    public async Task<ProductionRecipeDto> SaveRecipeAsync(ProductionRecipeDto dto)
    {
        ProductionRecipe? recipe = null;
        if (dto.Id > 0)
        {
            recipe = await _context.ProductionRecipes
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.Id == dto.Id);
        }

        if (recipe == null)
        {
            recipe = new ProductionRecipe
            {
                Code = string.IsNullOrWhiteSpace(dto.Code) ? $"BOM-{DateTime.UtcNow:yyyyMMddHHmmss}" : dto.Code,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductionRecipes.Add(recipe);
        }

        recipe.Name = dto.Name;
        recipe.ProducedProductId = dto.ProducedProductId;
        recipe.YieldQuantity = dto.YieldQuantity > 0 ? dto.YieldQuantity : 1;
        recipe.YieldUnitId = dto.YieldUnitId;
        recipe.Notes = dto.Notes;
        recipe.IsActive = dto.IsActive;

        // Update Ingredients
        recipe.Ingredients.Clear();
        decimal totalEstCost = 0;

        foreach (var ing in dto.Ingredients)
        {
            var product = await _context.WarehouseProducts.FindAsync(ing.IngredientProductId);
            decimal cost = product != null ? (product.LastPurchaseCost > 0 ? product.LastPurchaseCost : product.WeightedAverageCost) : 0;
            decimal lineCost = Math.Round(ing.Quantity * cost, 2);
            totalEstCost += lineCost;

            recipe.Ingredients.Add(new ProductionRecipeIngredient
            {
                IngredientProductId = ing.IngredientProductId,
                Quantity = ing.Quantity,
                UnitId = ing.UnitId,
                WastePercent = ing.WastePercent,
                EstimatedCost = lineCost,
                Notes = ing.Notes
            });
        }

        recipe.EstimatedBatchCost = totalEstCost;
        recipe.EstimatedUnitCost = recipe.YieldQuantity > 0 ? Math.Round(totalEstCost / recipe.YieldQuantity, 2) : totalEstCost;

        await _context.SaveChangesAsync();

        return (await GetRecipeByProductIdAsync(recipe.ProducedProductId))!;
    }

    public async Task<bool> DeleteRecipeAsync(int id)
    {
        var recipe = await _context.ProductionRecipes.FindAsync(id);
        if (recipe == null) return false;

        _context.ProductionRecipes.Remove(recipe);
        await _context.SaveChangesAsync();
        return true;
    }

    private static ProductionOrderDto MapToDto(ProductionOrder o)
    {
        return new ProductionOrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            WarehouseId = o.WarehouseId,
            WarehouseName = o.Warehouse?.NameAr ?? $"مخزن {o.WarehouseId}",
            ProducedProductId = o.ProducedProductId,
            ProducedProductName = o.ProducedProduct?.NameAr ?? $"صنف {o.ProducedProductId}",
            ProducedProductSku = o.ProducedProduct?.SKU,
            ProducedProductUnitName = o.ProducedProduct?.BaseUnit?.NameAr ?? "وحدة",
            ProductionRecipeId = o.ProductionRecipeId,
            ProductionRecipeName = o.ProductionRecipe?.Name,
            TargetQuantity = o.TargetQuantity,
            ActualQuantityProduced = o.ActualQuantityProduced,
            Status = (int)o.Status,
            OrderDate = o.OrderDate,
            CompletedDate = o.CompletedDate,
            TotalActualCost = o.TotalActualCost,
            UnitCost = o.UnitCost,
            Notes = o.Notes,
            CreatedByUserName = o.CreatedByUserName,
            Lines = o.Lines.Select(l => new ProductionOrderLineDto
            {
                Id = l.Id,
                ProductionOrderId = l.ProductionOrderId,
                IngredientProductId = l.IngredientProductId,
                IngredientProductName = l.IngredientProduct?.NameAr ?? $"خامة {l.IngredientProductId}",
                IngredientProductSku = l.IngredientProduct?.SKU,
                UnitName = l.IngredientProduct?.BaseUnit?.NameAr ?? "وحدة",
                PlannedQuantity = l.PlannedQuantity,
                ActualQuantityConsumed = l.ActualQuantityConsumed,
                UnitCost = l.UnitCost,
                TotalCost = l.TotalCost,
                AvailableStockAtExecution = l.AvailableStockAtExecution,
                Notes = l.Notes
            }).ToList()
        };
    }

    private static ProductionRecipeDto MapRecipeToDto(ProductionRecipe r)
    {
        return new ProductionRecipeDto
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name,
            ProducedProductId = r.ProducedProductId,
            ProducedProductName = r.ProducedProduct?.NameAr ?? $"صنف {r.ProducedProductId}",
            ProducedProductSku = r.ProducedProduct?.SKU,
            YieldQuantity = r.YieldQuantity,
            YieldUnitId = r.YieldUnitId ?? r.ProducedProduct?.BaseUnitId,
            YieldUnitName = r.YieldUnit?.NameAr ?? r.ProducedProduct?.BaseUnit?.NameAr ?? "وحدة",
            EstimatedBatchCost = r.EstimatedBatchCost,
            EstimatedUnitCost = r.EstimatedUnitCost,
            Notes = r.Notes,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            Ingredients = r.Ingredients.Select(i => new ProductionRecipeIngredientDto
            {
                Id = i.Id,
                ProductionRecipeId = i.ProductionRecipeId,
                IngredientProductId = i.IngredientProductId,
                IngredientProductName = i.IngredientProduct?.NameAr ?? $"خامة {i.IngredientProductId}",
                IngredientProductSku = i.IngredientProduct?.SKU,
                Quantity = i.Quantity,
                UnitId = i.UnitId ?? i.IngredientProduct?.BaseUnitId,
                UnitName = i.Unit?.NameAr ?? i.IngredientProduct?.BaseUnit?.NameAr ?? "وحدة",
                WastePercent = i.WastePercent,
                EstimatedCost = i.EstimatedCost,
                Notes = i.Notes
            }).ToList()
        };
    }
}
