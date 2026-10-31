using POS.Contract.Dtos.InventoryDtos;
using POS.Core.Services.Contract.InventoryServices;
using POS.Core.Entities.Item;

namespace POS.API.Controllers;

public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;
    private readonly IRecipeService _recipeService;
    private readonly IMapper _mapper;

    public InventoryController(IInventoryService inventoryService, IRecipeService recipeService, IMapper mapper)
    {
        _inventoryService = inventoryService;
        _recipeService = recipeService;
        _mapper = mapper;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllInventory()
    {
        var items = await _inventoryService.GetAllInventoryItemsAsync();
        var recipes = await _recipeService.GetAllRecipesAsync();
        var recipeItemIds = recipes.Select(r => r.MenuSalesItemId).ToHashSet();

        // Since we don't have automapper config for this new entity yet, we'll map manually or just return mapped DTOs if possible
        var dtos = items.Select(i => new InventoryItemDto
        {
            Id = i.Id,
            MenuSalesItemId = i.MenuSalesItemId,
            ItemNameAr = i.ItemNameAr ?? i.MenuSalesItem?.ArabicName,
            ItemNameEn = i.ItemNameEn ?? i.MenuSalesItem?.EnglishName,
            CurrentQuantity = i.CurrentQuantity,
            MinimumQuantity = i.MinimumQuantity,
            UnitId = i.UnitId,
            Unit = i.UnitNameAr ?? i.Unit?.ArabicName ?? i.Unit?.EnglishName,
            UnitNameAr = i.UnitNameAr ?? i.Unit?.ArabicName,
            UnitNameEn = i.UnitNameEn ?? i.Unit?.EnglishName,
            CategoryId = i.CategoryId ?? i.MenuSalesItem?.CategoryId,
            CategoryNameAr = i.CategoryNameAr,
            CategoryNameEn = i.CategoryNameEn,
            TrackInventory = i.TrackInventory,
            ItemTypeId = i.ItemTypeId,
            ItemTypeCode = i.ItemTypeCode,
            HasRecipe = recipeItemIds.Contains(i.MenuSalesItemId),
            UpdatedAt = i.UpdatedAt ?? i.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{itemId}")]
    public async Task<IActionResult> GetByItemId(int itemId)
    {
        var item = await _inventoryService.GetInventoryByItemIdAsync(itemId);
        if (item == null) return NotFound(new ApiResponse(404));

        return Ok(new InventoryItemDto
        {
            Id = item.Id,
            MenuSalesItemId = item.MenuSalesItemId,
            ItemNameAr = item.ItemNameAr ?? item.MenuSalesItem?.ArabicName,
            ItemNameEn = item.ItemNameEn ?? item.MenuSalesItem?.EnglishName,
            CurrentQuantity = item.CurrentQuantity,
            MinimumQuantity = item.MinimumQuantity,
            UnitId = item.UnitId,
            Unit = item.UnitNameAr ?? item.Unit?.ArabicName ?? item.Unit?.EnglishName,
            UnitNameAr = item.UnitNameAr ?? item.Unit?.ArabicName,
            UnitNameEn = item.UnitNameEn ?? item.Unit?.EnglishName,
            CategoryId = item.CategoryId ?? item.MenuSalesItem?.CategoryId,
            CategoryNameAr = item.CategoryNameAr,
            CategoryNameEn = item.CategoryNameEn,
            TrackInventory = item.TrackInventory,
            ItemTypeId = item.ItemTypeId,
            ItemTypeCode = item.ItemTypeCode,
            UpdatedAt = item.UpdatedAt ?? item.CreatedAt
        });
    }

    [HttpPost("UpdateStock")]
    public async Task<IActionResult> UpdateStock([FromBody] UpdateStockDto updateDto)
    {
        try
        {
            var result = await _inventoryService.UpdateStockAsync(
                updateDto.MenuSalesItemId, 
                updateDto.QuantityChange, 
                (TransactionType)updateDto.TransactionType,
                notes: updateDto.Notes,
                reason: updateDto.Reason,
                imagePaths: updateDto.ImagePaths,
                createdBy: updateDto.CreatedBy
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse(400, ex.Message));
        }
    }

    [HttpPost("SetOpeningStock")]
    public async Task<IActionResult> SetOpeningStock([FromBody] UpdateStockDto updateDto)
    {
        try
        {
            var result = await _inventoryService.SetOpeningStockAsync(
                updateDto.MenuSalesItemId, 
                updateDto.QuantityChange, 
                updateDto.Notes,
                createdBy: updateDto.CreatedBy
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse(400, ex.Message));
        }
    }

    [HttpPost("SetPhysicalStock")]
    public async Task<IActionResult> SetPhysicalStock([FromBody] UpdateStockDto updateDto)
    {
        try
        {
            var result = await _inventoryService.SetPhysicalStockAsync(
                updateDto.MenuSalesItemId, 
                updateDto.QuantityChange, 
                updateDto.Notes,
                createdBy: updateDto.CreatedBy
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse(400, ex.Message));
        }
    }

    [HttpPost("Initialize")]
    public async Task<IActionResult> Initialize([FromBody] InventoryItemDto initDto)
    {
        var result = await _inventoryService.InitializeInventoryAsync(
            initDto.MenuSalesItemId, 
            initDto.CurrentQuantity, 
            initDto.MinimumQuantity, 
            initDto.UnitId,
            initDto.TrackInventory
        );
        return Ok(result != null);
    }

    [HttpPost("InitializeAll")]
    public async Task<IActionResult> InitializeAll()
    {
        await _inventoryService.InitializeAllItemsAsync();
        return Ok(true);
    }

    [HttpGet("Transactions/{itemId}")]
    public async Task<IActionResult> GetTransactions(int itemId)
    {
        var transactions = await _inventoryService.GetTransactionsByItemIdAsync(itemId);
        var dtos = transactions.Select(t => new InventoryTransactionDto
        {
            Id = t.Id,
            QuantityChange = t.QuantityChange,
            ResultingQuantity = t.ResultingQuantity,
            Type = t.Type.ToString(),
            ReferenceId = t.ReferenceId,
            Reason = t.Reason,
            ImagePaths = string.Join(";", t.Images.Select(img => img.Base64Content)),
            Notes = t.Notes,
            CreatedBy = t.CreatedBy,
            CreatedAt = t.CreatedAt
        }).ToList();
        return Ok(dtos);
    }
}
