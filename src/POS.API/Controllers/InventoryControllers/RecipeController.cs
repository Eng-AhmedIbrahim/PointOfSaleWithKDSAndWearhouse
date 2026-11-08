using POS.Contract.Dtos.InventoryDtos;
using POS.Core.Services.Contract.InventoryServices;

namespace POS.API.Controllers.InventoryControllers;

public class RecipeController : BaseApiController
{
    private readonly IRecipeService _recipeService;
    private readonly IMapper _mapper;

    public RecipeController(IRecipeService recipeService, IMapper mapper)
    {
        _recipeService = recipeService;
        _mapper = mapper;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        var recipes = await _recipeService.GetAllRecipesAsync();
        var dtos = recipes.Select(r => new RecipeDto
        {
            Id = r.Id,
            MenuSalesItemId = r.MenuSalesItemId,
            MenuSalesItemName = r.MenuSalesItem?.ArabicName ?? r.MenuSalesItem?.EnglishName,
            RecipeName = r.RecipeName,
            IsActive = r.IsActive,
            Ingredients = r.Ingredients.Select(i => new RecipeIngredientDto
            {
                Id = i.Id,
                MenuSalesIngredientId = i.MenuSalesIngredientId,
                IngredientName = i.MenuSalesIngredient?.ArabicName ?? i.MenuSalesIngredient?.EnglishName,
                Quantity = i.Quantity,
                UnitId = i.UnitId,
                Unit = i.Unit?.ArabicName ?? i.Unit?.EnglishName
            }).ToList()
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("{itemId}")]
    public async Task<IActionResult> GetByItemId(int itemId)
    {
        var recipe = await _recipeService.GetRecipeByItemIdAsync(itemId);
        if (recipe == null) return NotFound();

        return Ok(new RecipeDto
        {
            Id = recipe.Id,
            MenuSalesItemId = recipe.MenuSalesItemId,
            MenuSalesItemName = recipe.MenuSalesItem?.ArabicName ?? recipe.MenuSalesItem?.EnglishName,
            RecipeName = recipe.RecipeName,
            IsActive = recipe.IsActive,
            Ingredients = recipe.Ingredients.Select(i => new RecipeIngredientDto
            {
                Id = i.Id,
                MenuSalesIngredientId = i.MenuSalesIngredientId,
                IngredientName = i.MenuSalesIngredient?.ArabicName ?? i.MenuSalesIngredient?.EnglishName,
                Quantity = i.Quantity,
                UnitId = i.UnitId,
                Unit = i.Unit?.ArabicName ?? i.Unit?.EnglishName
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RecipeDto dto)
    {
        var recipe = new Recipe
        {
            MenuSalesItemId = dto.MenuSalesItemId,
            RecipeName = dto.RecipeName,
            IsActive = true,
            Ingredients = dto.Ingredients.Select(i => new RecipeIngredient
            {
                MenuSalesIngredientId = i.MenuSalesIngredientId,
                Quantity = i.Quantity,
                UnitId = i.UnitId
            }).ToList()
        };
        await _recipeService.CreateRecipeAsync(recipe);
        return Ok(true);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] RecipeDto dto)
    {
        var existing = await _recipeService.GetRecipeByItemIdAsync(dto.MenuSalesItemId);
        if (existing == null) return NotFound();

        // تحديث بيانات الريسيبي
        existing.RecipeName = dto.RecipeName;
        existing.IsActive = dto.IsActive;

        // حذف المكونات القديمة وإضافة الجديدة
        existing.Ingredients.Clear();
        foreach (var i in dto.Ingredients)
        {
            existing.Ingredients.Add(new RecipeIngredient
            {
                RecipeId = existing.Id,
                MenuSalesIngredientId = i.MenuSalesIngredientId,
                Quantity = i.Quantity,
                UnitId = i.UnitId
            });
        }

        await _recipeService.UpdateRecipeAsync(existing);
        return Ok(true);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        return Ok(await _recipeService.DeleteRecipeAsync(id));
    }
}
