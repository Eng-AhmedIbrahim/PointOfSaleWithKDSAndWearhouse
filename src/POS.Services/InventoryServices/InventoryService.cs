using POS.Core.Services.Contract.InventoryServices;
using POS.Core.Specifications;
using POS.Core.Specifications.InventorySpecs;
using POS.Core.Services.Contract.PosFeatureServices;

namespace POS.Services.InventoryServices;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRecipeService _recipeService;
    private readonly IPosFeatureSettingsService _featureSettings;

    public InventoryService(IUnitOfWork unitOfWork, 
    IRecipeService recipeService, 
    IPosFeatureSettingsService featureSettings)
    {
        _unitOfWork = unitOfWork;
        _recipeService = recipeService;
        _featureSettings = featureSettings;
    }

    public async Task<InventoryItem?> GetInventoryByItemIdAsync(int menuSalesItemId)
    {
        try
        {
            var spec = new InventoryItemByMenuSalesItemIdSpecification(menuSalesItemId);
            return await _unitOfWork.Repository<InventoryItem>().GetByIdWithSpecificationTrackedAsync(spec);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting inventory for item {itemId}", menuSalesItemId);
            return null;
        }
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllInventoryItemsAsync()
    {
        try
        {
            return await _unitOfWork.Repository<InventoryItem>().GetAllAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting all inventory items");
            return new List<InventoryItem>();
        }
    }

    public async Task ConsumeItemStockAsync(int menuSalesItemId, decimal quantity, TransactionType type = TransactionType.Sale, string? referenceId = null)
    {
        await ConsumeRecursiveInternal(menuSalesItemId, quantity, type, referenceId, 0);
    }

    private async Task ConsumeRecursiveInternal(int menuSalesItemId, decimal quantity, TransactionType type, string? referenceId, int depth)
    {
        if (depth > 5) return; // Prevention for infinite recipe loops
        if (!await _featureSettings.IsFeatureEnabledAsync("EnableInventoryTracking")) return;

        try 
        {
            var recipe = await _recipeService.GetRecipeByItemIdAsync(menuSalesItemId);
            
            string actionWord = type == TransactionType.Void ? "Returned" : "Consumed";
            decimal factor = type == TransactionType.Void ? 1 : -1;

            // 1. Deduct the item itself if it's tracked
            var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
            if (inventory != null && inventory.TrackInventory)
            {
                await UpdateStockAsync(
                    menuSalesItemId, 
                    quantity * factor, 
                    type, 
                    referenceId, 
                    $"Item {actionWord.ToLower()} x {quantity}");
            }

            // 2. Deduct ingredients if there's a recipe
            if (recipe != null && recipe.Ingredients.Any())
            {
                foreach (var ingredient in recipe.Ingredients)
                {
                    decimal totalRequiredAction = ingredient.Quantity * quantity;
                    
                    // Check if this ingredient itself has a recipe (Recursive deduction)
                    var ingredientRecipe = await _recipeService.GetRecipeByItemIdAsync(ingredient.MenuSalesIngredientId);
                    var ingredientInventory = await GetInventoryByItemIdAsync(ingredient.MenuSalesIngredientId);

                    // --- UNIT CONVERSION LOGIC ---
                    Unit? ingredientUnit = ingredient.Unit;
                    if (ingredientUnit == null && ingredient.UnitId.HasValue)
                        ingredientUnit = await _unitOfWork.Repository<Unit>().GetByIdAsync(ingredient.UnitId.Value);

                    Unit? inventoryUnit = null;
                    if (ingredientInventory != null && ingredientInventory.UnitId.HasValue)
                        inventoryUnit = await _unitOfWork.Repository<Unit>().GetByIdAsync(ingredientInventory.UnitId.Value);

                    // Convert required amount based on Recipe Unit -> Inventory Unit
                    totalRequiredAction = UnitConverter.Convert(totalRequiredAction, ingredientUnit, inventoryUnit);
                    // -----------------------------

                    // If ingredient has a recipe, we always process it recursively to handle multi-level recipes.
                    if (ingredientRecipe != null && ingredientRecipe.Ingredients.Any())
                    {
                        // Note: ConsumeRecursiveInternal handles checking if the ingredient itself is tracked
                        // so we don't need to call UpdateStockAsync here for the ingredient.
                        await ConsumeRecursiveInternal(ingredient.MenuSalesIngredientId, totalRequiredAction, type, referenceId, depth + 1);
                    }
                    else
                    {
                        // If no recipe, just update the stock of the ingredient itself
                        await UpdateStockAsync(
                            ingredient.MenuSalesIngredientId, 
                            totalRequiredAction * factor, 
                            type, 
                            referenceId, 
                            $"{actionWord} for {recipe.RecipeName} x {quantity}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error consuming stock for item {itemId}", menuSalesItemId);
        }
    }

    public async Task<bool> UpdateStockAsync(int menuSalesItemId, decimal quantityChange, TransactionType type, string? referenceId = null, string? notes = null, string? reason = null, string? imagePaths = null, string? createdBy = null)
    {
        try
        {
            var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
            if (inventory == null) 
            {
                Log.Warning("Inventory not found for item {itemId}", menuSalesItemId);
                return false;
            }

            // Manual transactions (Adjustment, Damage, Waste, PhysicalCount, OpeningStock) 
            // should always allow updating the quantity. 
            // Only automatic deductions (Sale, Recipe) should respect TrackInventory.
            bool isManual = type == TransactionType.Adjustment || 
                            type == TransactionType.Damage || 
                            type == TransactionType.Waste || 
                            type == TransactionType.OpeningStock ||
                            type == TransactionType.PhysicalCount;

            if (!inventory.TrackInventory && !isManual) 
            {
                Log.Information("Skipping stock update for untracked item {itemId}", menuSalesItemId);
                return true; // Skipping is "success" for automatic deduction
            }

            inventory.CurrentQuantity += quantityChange;
            inventory.UpdatedAt = DateTime.UtcNow;

            var transaction = new InventoryTransaction
            {
                InventoryItemId = inventory.Id,
                QuantityChange = quantityChange,
                ResultingQuantity = inventory.CurrentQuantity,
                Type = type,
                ReferenceId = referenceId,
                Notes = notes,
                Reason = reason,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(imagePaths))
            {
                var paths = imagePaths.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in paths)
                {
                    transaction.Images.Add(new InventoryTransactionImage { Base64Content = path });
                }
            }

            _unitOfWork.Repository<InventoryItem>().Update(inventory);
            await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction);
            
            var result = await _unitOfWork.CompleteAsync();
            if (result == 0) throw new Exception("Database failed to save changes. Please try again.");
            
            Log.Information("[INVENTORY] UpdateStockAsync success for item {itemId}", menuSalesItemId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating stock for item {itemId}", menuSalesItemId);
            throw; // Re-throw to be caught by Controller for proper error reporting
        }
    }

    public async Task<InventoryItem> InitializeInventoryAsync(int menuSalesItemId, decimal initialQuantity, decimal minQuantity, int? unitId, bool? trackInventory = null)
    {
        var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
        
        var branch = (await _unitOfWork.Repository<Branch>().GetAllAsync()).FirstOrDefault();
        var branchId = branch?.Id;

        // Fetch metadata
        var item = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(menuSalesItemId);
        Unit? unit = unitId.HasValue ? await _unitOfWork.Repository<Unit>().GetByIdAsync(unitId.Value) : null;
        Category? category = item?.CategoryId.HasValue == true ? await _unitOfWork.Repository<Category>().GetByIdAsync(item.CategoryId.Value) : null;

        if (inventory == null)
        {
            inventory = new InventoryItem
            {
                MenuSalesItemId = menuSalesItemId,
                CurrentQuantity = initialQuantity,
                MinimumQuantity = minQuantity,
                UnitId = unitId,
                TrackInventory = item?.IsInventory ?? true,
                CreatedAt = DateTime.UtcNow,
                BranchId = branchId
            };
            await _unitOfWork.Repository<InventoryItem>().AddAsync(inventory);
            
            var transaction = new InventoryTransaction
            {
                InventoryItem = inventory,
                QuantityChange = initialQuantity,
                ResultingQuantity = initialQuantity,
                Type = TransactionType.StockIn,
                Notes = "Initial setup",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction);
        }
        else 
        {
            inventory.UnitId = unitId;
            inventory.MinimumQuantity = minQuantity;
            inventory.TrackInventory = trackInventory ?? inventory.TrackInventory;
        }

        // Keep metadata updated
        if (item != null)
        {
            inventory.ItemNameAr = item.ArabicName;
            inventory.ItemNameEn = item.EnglishName;
            inventory.CategoryId = item.CategoryId;
            
            if (category != null)
            {
                inventory.CategoryNameAr = category.ArabicName;
                inventory.CategoryNameEn = category.EnglishName;
            }
        }

        if (unit != null)
        {
            inventory.UnitNameAr = unit.ArabicName;
            inventory.UnitNameEn = unit.EnglishName;
        }

        if (item != null)
        {
            inventory.ItemTypeId = item.ItemTypeId;
            if (item.ItemTypeId.HasValue)
            {
                var type = await _unitOfWork.Repository<ItemType>().GetByIdAsync(item.ItemTypeId.Value);
                inventory.ItemTypeCode = type?.Code;
            }
        }

        if (inventory.Id > 0)
        {
            _unitOfWork.Repository<InventoryItem>().Update(inventory);
        }

        await _unitOfWork.CompleteAsync();
        return inventory;
    }

    public async Task InitializeAllItemsAsync()
    {
        try
        {
            var branch = (await _unitOfWork.Repository<Branch>().GetAllAsync()).FirstOrDefault();
            var branchId = branch?.Id;

            // Load all items with categories and units to populate denormalized fields
            var allItems = await _unitOfWork.Repository<MenuSalesItems>().GetAllAsync();
            var allUnits = await _unitOfWork.Repository<Unit>().GetAllAsync();
            var allCategories = await _unitOfWork.Repository<Category>().GetAllAsync();

            var unitMap = allUnits.ToDictionary(u => u.Id);
            var categoryMap = allCategories.ToDictionary(c => c.Id);

            var existingInventory = await _unitOfWork.Repository<InventoryItem>().GetAllAsync();
            var inventoryMap = existingInventory.ToDictionary(x => x.MenuSalesItemId);

            // Load all item types to identify services
            var allItemTypes = await _unitOfWork.Repository<ItemType>().GetAllAsync();
            var serviceTypeIds = allItemTypes.Where(t => t.Code == "Service").Select(t => t.Id).ToList();

            Console.WriteLine($"[INVENTORY] Starting InitializeAllItemsAsync. Found {allItems.Count} items.");
            foreach (var item in allItems)
            {
                // Skip service items
                if (item.ItemTypeId.HasValue && serviceTypeIds.Contains(item.ItemTypeId.Value))
                {
                    Console.WriteLine($"[INVENTORY] Skipping service item: {item.ArabicName}");
                    continue;
                }

                // If IsInventory is false, we only skip if it's NOT a new record being created.
                // This ensures existing items get an inventory record if they don't have one.
                if (!item.IsInventory && inventoryMap.ContainsKey(item.Id))
                {
                     // If it's already in inventory but marked as NOT inventory now, we could skip or keep.
                     // For now, let's just keep it if it exists.
                }

                if (!inventoryMap.TryGetValue(item.Id, out var inventory))
                {
                    Console.WriteLine($"[INVENTORY] Creating new inventory record for: {item.ArabicName}");
                    inventory = new InventoryItem
                    {
                        MenuSalesItemId = item.Id,
                        CurrentQuantity = 0,
                        MinimumQuantity = 0,
                        TrackInventory = item.IsInventory,
                        CreatedAt = DateTime.UtcNow,
                        BranchId = branchId,
                        ItemTypeId = item.ItemTypeId ?? 1 // Default to SaleItem if NULL
                    };

                    // Set TypeCode based on ID
                    if (inventory.ItemTypeId.HasValue)
                    {
                        var type = allItemTypes.FirstOrDefault(t => t.Id == inventory.ItemTypeId.Value);
                        inventory.ItemTypeCode = type?.Code;
                    }

                    await _unitOfWork.Repository<InventoryItem>().AddAsync(inventory);
                }
                else
                {
                    // Update existing records that have NULL values
                    bool changed = false;
                    if (!inventory.ItemTypeId.HasValue)
                    {
                        inventory.ItemTypeId = item.ItemTypeId ?? 1;
                        changed = true;
                    }
                    
                    if (string.IsNullOrEmpty(inventory.ItemTypeCode) && inventory.ItemTypeId.HasValue)
                    {
                        var type = allItemTypes.FirstOrDefault(t => t.Id == inventory.ItemTypeId.Value);
                        inventory.ItemTypeCode = type?.Code;
                        changed = true;
                    }

                    if (inventory.TrackInventory != item.IsInventory)
                    {
                        inventory.TrackInventory = item.IsInventory;
                        changed = true;
                    }

                    if (changed)
                    {
                        _unitOfWork.Repository<InventoryItem>().Update(inventory);
                    }
                }

                // Always update metadata to keep it in sync
                inventory.ItemNameAr = item.ArabicName;
                inventory.ItemNameEn = item.EnglishName;
                inventory.CategoryId = item.CategoryId;
                
                if (item.CategoryId.HasValue && categoryMap.TryGetValue(item.CategoryId.Value, out var category))
                {
                    inventory.CategoryNameAr = category.ArabicName;
                    inventory.CategoryNameEn = category.EnglishName;
                }

                if (inventory.UnitId.HasValue && unitMap.TryGetValue(inventory.UnitId.Value, out var unit))
                {
                    inventory.UnitNameAr = unit.ArabicName;
                    inventory.UnitNameEn = unit.EnglishName;
                }

                // Update ItemType denormalized fields
                inventory.ItemTypeId = item.ItemTypeId;
                if (item.ItemTypeId.HasValue && allItemTypes.Any())
                {
                    var type = allItemTypes.FirstOrDefault(t => t.Id == item.ItemTypeId.Value);
                    if (type != null)
                        inventory.ItemTypeCode = type.Code;
                }
                
                if (inventory.Id > 0)
                {
                    _unitOfWork.Repository<InventoryItem>().Update(inventory);
                }
            }
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing all inventory items with metadata");
        }
    }

    public async Task<bool> SetOpeningStockAsync(int menuSalesItemId, decimal openingQuantity, string? notes = null, string? createdBy = null)
    {
        try
        {
            var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
            if (inventory == null) return false;

            decimal difference = openingQuantity - inventory.CurrentQuantity;
            return await UpdateStockAsync(menuSalesItemId, difference, TransactionType.OpeningStock, notes: notes, createdBy: createdBy);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting opening stock for item {itemId}", menuSalesItemId);
            return false;
        }
    }

    public async Task<bool> SetPhysicalStockAsync(int menuSalesItemId, decimal actualQuantity, string? notes = null, string? createdBy = null)
    {
        try
        {
            var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
            if (inventory == null) return false;

            decimal difference = actualQuantity - inventory.CurrentQuantity;
            return await UpdateStockAsync(menuSalesItemId, difference, TransactionType.PhysicalCount, notes: notes, createdBy: createdBy);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error setting physical stock for item {itemId}", menuSalesItemId);
            return false;
        }
    }

    public async Task<IReadOnlyList<InventoryTransaction>> GetTransactionsByItemIdAsync(int menuSalesItemId)
    {
        var inventory = await GetInventoryByItemIdAsync(menuSalesItemId);
        if (inventory == null) return new List<InventoryTransaction>();

        var spec = new InventoryTransactionsByInventoryIdSpecification(inventory.Id);
        return await _unitOfWork.Repository<InventoryTransaction>().GetAllWithSpecificationAsync(spec);
    }
}
