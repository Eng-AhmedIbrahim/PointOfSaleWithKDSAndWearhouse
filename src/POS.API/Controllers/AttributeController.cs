using POS.Contract.Dtos.AttributeDtos;

namespace POS.API.Controllers;

public class AttributeController : BaseApiController
{

    private readonly IAttributeService _attributeService;
    private readonly IMapper _mapper;

    public AttributeController(IAttributeService attributeService, IMapper mapper)
    {
        _attributeService = attributeService;
        _mapper = mapper;
    }

    [ProducesResponseType(typeof(AttributeToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> CreateAttributeAsync([FromBody] CreateAttributeDto attributeDto)
    {
        if (attributeDto is null)
            return BadRequest(new ApiResponse(400));

        var mappedAttribute = _mapper.Map<CreateAttributeDto, Attributes>(attributeDto);

        if (mappedAttribute is null)
            return BadRequest(new ApiResponse(400));

        var attribute = await _attributeService.CreateAttributeAsync(mappedAttribute);
        if (attribute is null)
            return BadRequest(new ApiResponse(400));
        var attributeToReturn = _mapper.Map<Attributes, AttributeToReturnDto>(attribute);

        return Ok(attributeToReturn);
    }

    [ProducesResponseType(typeof(AttributeToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("{attributeId}")]
    public async Task<IActionResult?> GetAttributeById(int attributeId)
    {
        var specs = new AttributeWithIncludeSpecs(attributeId);
        var attribute = await _attributeService.GetAttributeByIdWithSpecAsync(specs);

        if (attribute is null)
            return NotFound(new ApiResponse(404,$"Attribute With Id {attributeId} Not Found"));

        var attributeToReturn = _mapper.Map<Attributes, AttributeToReturnDto>(attribute);
        return Ok(attributeToReturn);
    }

    [ProducesResponseType(typeof(IReadOnlyList<AttributeToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet]
    public async Task<IActionResult?> GetAllAttributes()
    {
        var specs = new AttributeWithIncludeSpecs();
        var attributes = await _attributeService.GetAllAttributeWithSpecsAsync(specs);
        var attributeToReturn = _mapper.Map<IReadOnlyList<Attributes>, IReadOnlyList<AttributeToReturnDto>>(attributes);

        if (attributeToReturn is null)
            return NotFound(new ApiResponse(404));

        return Ok(attributeToReturn);
    }

    [ProducesResponseType(typeof(IReadOnlyList<AttributeToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpPut]
    public async Task<IActionResult?> UpdateAttribute(UpdatedAttributeDto newAttribute)
    {
        var specs = new AttributeWithIncludeSpecs(newAttribute.Id);
        var oldAttribute = await _attributeService.GetAttributeByIdWithSpecAsync(specs);
        if (oldAttribute is null)
            return NotFound(new ApiResponse(404));

        var  mappedNewAttribute= _mapper.Map<UpdatedAttributeDto, Attributes>(newAttribute);
        var attribute = await _attributeService.UpdateAttributeAsync(oldAttribute, mappedNewAttribute);
        if(attribute is null)
            return NotFound(new ApiResponse(404));

        var attributeToReturn = _mapper.Map<Attributes, AttributeToReturnDto>(attribute);

        return Ok(attributeToReturn);
    }

    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpDelete("DeleteAttribute/{attributeId}")]
    public async Task<IActionResult> DeleteAttribute(int attributeId)
    {
        var attribute = await _attributeService.GetAttributeByIdAsync(attributeId);
        if (attribute is null)
            return NotFound(new ApiResponse(404));

        var result = await _attributeService.DeleteAttribute(attribute);
        if (result is true)
            return Ok("Deleted Successfully");

        return BadRequest(new ApiResponse(400));
    }

    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpDelete("DeleteAttributeItem/{attributeItemId}")]
    public async Task<IActionResult> DeleteAttributeItem(int attributeItemId)
    {
        var attributeItem = await _attributeService.GetAttributeItemByIdAsync(attributeItemId);
        if (attributeItem is null)
            return NotFound(new ApiResponse(404, $"Attribute With Id {attributeItemId} Not Found"));

        var result = await _attributeService.DeleteAttributeItem(attributeItem);

        if (result is true)
            return Ok("Deleted Successfully");

        return BadRequest(new ApiResponse(400));
    }
}