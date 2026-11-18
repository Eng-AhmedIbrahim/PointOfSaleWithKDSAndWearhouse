using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Contract.Dtos.InventoryDtos;
using POS.Core.Services.Contract.InventoryServices;

namespace POS.API.Controllers.Warehouse;

[ApiController]
[Route("api/warehouse/production")]
[Route("api/production")]
[AllowAnonymous]
public class ProductionController : ControllerBase
{
    private readonly IProductionService _productionService;

    public ProductionController(IProductionService productionService)
    {
        _productionService = productionService;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] int? warehouseId)
    {
        var list = await _productionService.GetProductionOrdersAsync(warehouseId);
        return Ok(list);
    }

    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _productionService.GetProductionOrderByIdAsync(id);
        if (order == null) return NotFound(new { message = "أمر الإنتاج غير موجود" });
        return Ok(order);
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateProductionOrderRequest request)
    {
        if (request.WarehouseId <= 0 || request.ProducedProductId <= 0 || request.TargetQuantity <= 0)
            return BadRequest(new { message = "بيانات أمر الإنتاج غير مكتملة" });

        try
        {
            var result = await _productionService.CreateProductionOrderAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("orders/{id}/complete")]
    public async Task<IActionResult> CompleteOrder(int id, [FromQuery] decimal? actualQty)
    {
        var success = await _productionService.CompleteProductionOrderAsync(id, actualQty ?? 0, User?.Identity?.Name);
        if (!success) return BadRequest(new { message = "فشل في إتمام أمر الإنتاج" });
        return Ok(new { success = true, message = "تم تنفيذ الإنتاج وإيداع الكميات في المخزن بنجاح" });
    }

    [HttpPost("orders/{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, [FromQuery] string? reason)
    {
        var success = await _productionService.CancelProductionOrderAsync(id, reason);
        if (!success) return BadRequest(new { message = "لا يمكن إلغاء أمر إنتاج مكتمل" });
        return Ok(new { success = true, message = "تم إلغاء أمر الإنتاج" });
    }

    [HttpGet("recipes")]
    public async Task<IActionResult> GetRecipes()
    {
        var list = await _productionService.GetRecipesAsync();
        return Ok(list);
    }

    [HttpGet("recipes/by-product/{productId}")]
    public async Task<IActionResult> GetRecipeByProduct(int productId)
    {
        var recipe = await _productionService.GetRecipeByProductIdAsync(productId);
        if (recipe == null) return NotFound(new { message = "لا توجد وصفة إنتاج مسجلة لهذا الصنف" });
        return Ok(recipe);
    }

    [HttpPost("recipes")]
    public async Task<IActionResult> SaveRecipe([FromBody] ProductionRecipeDto dto)
    {
        if (dto.ProducedProductId <= 0)
            return BadRequest(new { message = "يرجى تحديد الصنف المراد إنتاجه" });

        var result = await _productionService.SaveRecipeAsync(dto);
        return Ok(result);
    }

    [HttpDelete("recipes/{id}")]
    public async Task<IActionResult> DeleteRecipe(int id)
    {
        var success = await _productionService.DeleteRecipeAsync(id);
        if (!success) return NotFound();
        return Ok(new { success = true });
    }

    [HttpGet("calculate-ingredients")]
    public async Task<IActionResult> CalculateIngredients([FromQuery] int warehouseId, [FromQuery] int productId, [FromQuery] decimal targetQty)
    {
        if (warehouseId <= 0 || productId <= 0 || targetQty <= 0)
            return BadRequest(new { message = "المعاملات غير صحيحة" });

        var ingredients = await _productionService.CalculateRequiredIngredientsAsync(warehouseId, productId, targetQty);
        return Ok(ingredients);
    }
}
