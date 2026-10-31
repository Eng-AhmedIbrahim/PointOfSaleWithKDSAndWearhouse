namespace POS.API.Controllers;

public class ItemController : BaseApiController
{
    private readonly IAttributeService _attributeService;
    private readonly IMenuSalesItemService _itemService;
    private readonly IMapper _mapper;

    public ItemController(IAttributeService attributeService, IMenuSalesItemService menuSalesItemService, IMapper mapper)
    {
        _attributeService = attributeService;
        _itemService = menuSalesItemService;
        _mapper = mapper;
    }

    [ProducesResponseType(typeof(MenuSalesItemsToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> CreateMenuItemAsync([FromBody] MenuSalesItemsDto itemDto)
    {
        string logoPath = "";
        if (itemDto is null)
            return BadRequest(new ApiResponse(400));

        var mappedItem = _mapper.Map<MenuSalesItemsDto, MenuSalesItems>(itemDto);

        if (mappedItem is null)
            return BadRequest(new ApiResponse(400));

        if (itemDto.Image is not null)
        {
            logoPath = DocumentSetting.UploadFile(itemDto.Image, "Imgs");
            mappedItem.ImagePath = logoPath;
        }

        var item = await _itemService.CreateItemAsync(mappedItem);

        if (item is null)
            return BadRequest(new ApiResponse(400));

        var itemToReturn = _mapper.Map<MenuSalesItems, MenuSalesItemsToReturnDto>(item);

        return Ok(itemToReturn);
    }

    //[Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<MenuSalesItemsToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("GetAllItems")]
    public async Task<IActionResult?> GetAllItems()
    {
        var items = await _itemService.GetAllItemsAsync();
        if (items is null)
            return NotFound(new ApiResponse(404));

        var mappedItems = _mapper.Map<IReadOnlyList<MenuSalesItems>, IReadOnlyList<MenuSalesItemsToReturnDto>>(items);

        var aggregatedItems = mappedItems.Select(mappedItem =>
        {
            var aggregatedAttributes = mappedItem.Attributes
                .GroupBy(attr => attr.AppearanceIndex)
                .Select(group => new MenuSalesItemAttributes
                {
                    AppearanceIndex = group.Key,
                    GroupItems = group.SelectMany(g => g.GroupItems).ToList()
                })
                .ToList();

            mappedItem.Attributes = aggregatedAttributes;
            return mappedItem;
        }).ToList();

        return Ok(aggregatedItems);
    }

    [ProducesResponseType(typeof(IReadOnlyList<ItemsClassificationsDto>), StatusCodes.Status200OK)]
    [HttpGet("GetClassifications")]
    public async Task<IActionResult> GetClassifications()
    {
        var classifications = await _itemService.GetAllClassificationsAsync();
        if (classifications is null)
            return NotFound(new ApiResponse(404));

        var mapped = _mapper.Map<IReadOnlyList<ItemsClassifications>, IReadOnlyList<ItemsClassificationsDto>>(classifications);
        return Ok(mapped);
    }

    [ProducesResponseType(typeof(ItemsClassificationsDto), StatusCodes.Status200OK)]
    [HttpPost("CreateClassification")]
    public async Task<IActionResult> CreateClassification([FromBody] ItemsClassificationsDto classificationDto)
    {
        if (classificationDto is null) return BadRequest(new ApiResponse(400));
        var mapped = _mapper.Map<ItemsClassificationsDto, ItemsClassifications>(classificationDto);
        var result = await _itemService.CreateClassificationAsync(mapped);
        if (result is null) return BadRequest(new ApiResponse(400, "Failed to create classification"));
        return Ok(_mapper.Map<ItemsClassifications, ItemsClassificationsDto>(result));
    }

    [ProducesResponseType(typeof(ItemsClassificationsDto), StatusCodes.Status200OK)]
    [HttpPut("UpdateClassification")]
    public async Task<IActionResult> UpdateClassification([FromBody] ItemsClassificationsDto classificationDto)
    {
        if (classificationDto is null) return BadRequest(new ApiResponse(400));
        var old = await _itemService.GetClassificationByIdAsync(classificationDto.Id);
        if (old is null) return NotFound(new ApiResponse(404));

        var mapped = _mapper.Map<ItemsClassificationsDto, ItemsClassifications>(classificationDto);
        var result = await _itemService.UpdateClassificationAsync(old, mapped);
        if (result is null) return BadRequest(new ApiResponse(400, "Failed to update classification"));
        return Ok(_mapper.Map<ItemsClassifications, ItemsClassificationsDto>(result));
    }

    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [HttpDelete("DeleteClassification/{id}")]
    public async Task<IActionResult> DeleteClassification(int id)
    {
        var old = await _itemService.GetClassificationByIdAsync(id);
        if (old is null) return NotFound(new ApiResponse(404));

        var result = await _itemService.DeleteClassification(old);
        if (result) return Ok(true);
        return BadRequest(new ApiResponse(400, "Failed to delete classification"));
    }

    [ProducesResponseType(typeof(IReadOnlyList<MenuSalesItemsToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("{itemId}")]
    public async Task<IActionResult?> GetItemById([FromRoute] int itemId)
    {
        var item = await _itemService.GetItemByIdAsync(itemId);

        if (item is null)
            return NotFound(new ApiResponse(404));


        var mappedItem = _mapper.Map<MenuSalesItems, MenuSalesItemsToReturnDto>(item);
        var aggregatedAttributes = mappedItem.Attributes
            .GroupBy(attr => attr.AppearanceIndex)
            .Select(group => new MenuSalesItemAttributes
            {
                AppearanceIndex = group.Key,
                GroupItems = group.SelectMany(g => g.GroupItems).ToList()
            })
            .ToList();

        mappedItem.Attributes = aggregatedAttributes;

        return Ok(mappedItem);
    }

    [ProducesResponseType(typeof(MenuSalesItemsToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpPut]
    public async Task<IActionResult?> UpdateItem([FromBody] UpdatedItemDto newItem)
    {
        string logoPath = "";
        
        // Try to get the existing item - use spec with includes for update
        var oldItem = await _itemService.GetItemByIdAsync(newItem.ItemId);
        
        // If spec-based lookup fails (might fail if no Attribute relation), try simple lookup
        if (oldItem is null)
        {
            // Fallback: search by ID without spec
            return NotFound(new ApiResponse(404, $"Item with ID {newItem.ItemId} not found"));
        }

        var mappedNewItem = _mapper.Map<UpdatedItemDto, MenuSalesItems>(newItem);
        if (newItem.Image is not null)
        {
            DocumentSetting.DeleteFile(oldItem?.ImagePath ?? string.Empty);
            logoPath = DocumentSetting.UploadFile(newItem.Image, "Imgs");
            mappedNewItem.ImagePath = logoPath;
        }

        var item = await _itemService.UpdateItemAsync(oldItem ?? new(), mappedNewItem);
        if (item is null)
            return BadRequest(new ApiResponse(400, "Failed to update item"));

        var itemToReturn = _mapper.Map<MenuSalesItems, MenuSalesItemsToReturnDto>(item);

        return Ok(itemToReturn);
    }

    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpDelete("{itemId}")]
    public async Task<IActionResult> DeleteItem([Required] int itemId)
    {
        var item = await _itemService.GetItemByIdAsync(itemId);
        if (item is null)
            return NotFound(new ApiResponse(404));

        var result = await _itemService.DeleteItem(item);
        if (result is true)
        {
            DocumentSetting.DeleteFile(item?.ImagePath ?? string.Empty);
            return Ok("Deleted Successfully");
        }

        return BadRequest(new ApiResponse(400));
    }


    [ProducesResponseType(typeof(MenuSalesItemsToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpPost("AddAttributeToItem")]
    public async Task<IActionResult?> AddAttributeToItem([Required] int attributeId, [Required] int ItemId)
    {
        var item = await _itemService.AddAttributeToItem(attributeId, ItemId);

        if (item is null)
            return NotFound(new ApiResponse(404));

        var itemToReturn = _mapper.Map<MenuSalesItems, MenuSalesItemsToReturnDto>(item);
        return Ok(itemToReturn);
    }


    [ProducesResponseType(typeof(IReadOnlyList<MenuSalesItemsToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("GetItemsByCategoryId")]
    public async Task<IActionResult> GetItemsByCatId([Required]int catId)
    {
        // Fetch items by category ID
        var items = await _itemService.GetItemByCategoryIdAsync(catId);
        if (items is null || !items.Any())
        {
            Log.Information($"No Items Found Has CatId={catId}");
            return NotFound(new ApiResponse(404, $"No Items Found Has CatId={catId}"));
        }

        // Map items to DTO
        var mappedItems = _mapper.Map<IReadOnlyList<MenuSalesItems>, IReadOnlyList<MenuSalesItemsToReturnDto>>(items);

        // Aggregate attributes for each item
        foreach (var item in mappedItems)
        {
            var aggregatedAttributes = item.Attributes
                .GroupBy(attr => attr.AppearanceIndex)
                .Select(group => new MenuSalesItemAttributes
                {
                    AppearanceIndex = group.Key,
                    GroupItems = group.SelectMany(g => g.GroupItems).ToList()
                })
                .ToList();

            item.Attributes = aggregatedAttributes;
        }

        return Ok(mappedItems);
    }

    [ProducesResponseType(typeof(IReadOnlyList<ItemTypeDto>), StatusCodes.Status200OK)]
    [HttpGet("GetItemTypes")]
    public async Task<IActionResult> GetItemTypes()
    {
        var types = await _itemService.GetAllItemTypesAsync();
        if (types is null)
            return NotFound(new ApiResponse(404));

        var mapped = _mapper.Map<IReadOnlyList<ItemType>, IReadOnlyList<ItemTypeDto>>(types);
        return Ok(mapped);
    }
}