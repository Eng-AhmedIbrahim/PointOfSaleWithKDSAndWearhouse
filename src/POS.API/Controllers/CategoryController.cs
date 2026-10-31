using POS.Core.Entities.Categorie;

namespace POS.API.Controllers;

public class CategoryController : BaseApiController
{
    private readonly ICategoryService _categoryService;
    private readonly IMapper _mapper;

    public CategoryController(ICategoryService categoryService , IMapper mapper)
    {
        _categoryService = categoryService;
        _mapper = mapper;
    }

    [ProducesResponseType(typeof(CategoryToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> CreateCategoryAsync([FromBody]CategoryDto categoryDto)
    {
        if (categoryDto is null)
            return BadRequest(new ApiResponse(400,"Category ArabicName,EnglishName Is Required"));

        var mappedCategory = _mapper.Map<CategoryDto, Category>(categoryDto);

        if (mappedCategory is null)
            return BadRequest(new ApiResponse(400));

        var category = await _categoryService.CreateCategoryAsync(mappedCategory)!;

        if (category is null)
            return BadRequest(new ApiResponse(400));
 
        var categoryToReturn = _mapper.Map<Category, CategoryToReturnDto>(category);

        return Ok(categoryToReturn);
    }

    [ProducesResponseType(typeof(IReadOnlyList<CategoryToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("GetAllCategories")]
    public async Task<IActionResult?> GetAllCategories()
    {
        var categories = await _categoryService.GetCategoriesAsync();
        if (categories is null)
            return NotFound(new ApiResponse(404));

        var categoriesToReturn = _mapper.Map<IReadOnlyList<Category>, IReadOnlyList<CategoryToReturnDto>>(categories);

        return Ok(categoriesToReturn);
    }

    [ProducesResponseType(typeof(IReadOnlyList<CategoryToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("{categoryId}")]
    public async Task<IActionResult?> GetCategoryById(int categoryId)
    {
        var category = await _categoryService.GetCategoryByIdAsync(categoryId);
        if (category is null)
            return NotFound(new ApiResponse(404));


        var categoryToReturn = _mapper.Map<Category, CategoryToReturnDto>(category);
        return Ok(categoryToReturn);
    }

    [ProducesResponseType(typeof(CategoryToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpPut]
    public async Task<IActionResult?> UpdateCategory([FromBody] UpdatedCategoryDto newCategory)
    {
        var oldCategory = await _categoryService.GetCategoryByIdAsync(newCategory.Id);
        if (oldCategory is null)
            return NotFound(new ApiResponse(404));

        var mappedNewCategory = _mapper.Map<UpdatedCategoryDto, Category>(newCategory);

        var category = await _categoryService.UpdateCategory(oldCategory, mappedNewCategory);

        var categoryToReturn = _mapper.Map<Category, CategoryToReturnDto>(category);

        return Ok(categoryToReturn);
    }

    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpDelete("{categoryId}")]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var category = await _categoryService.GetCategoryByIdAsync(categoryId);
        if (category is null)
            return NotFound(new ApiResponse(404));

        var result = await _categoryService.DeleteCategory(category);
        if (result is true)
            return Ok("Deleted Successfully");

        return BadRequest(new ApiResponse(400));
    }
}