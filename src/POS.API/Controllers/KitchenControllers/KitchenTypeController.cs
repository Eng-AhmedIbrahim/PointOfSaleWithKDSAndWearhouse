namespace POS.API.Controllers.KitchenControllers;

public class KitchenTypeController : BaseApiController
{
    private readonly IKitchenServices _kitchenServices;
    private readonly IMapper _mapper;

    public KitchenTypeController(IKitchenServices kitchenServices, IMapper mapper)
    {
        _kitchenServices = kitchenServices;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllKitchenTypesAsync()
    {
        var kitchenTypes = await _kitchenServices.GetAllKitchenTypesAsync();
        if (kitchenTypes == null || !kitchenTypes.Any())
            return NotFound("No kitchen types found.");

        var kitchenTypesToReturn = _mapper.Map<IReadOnlyList<KitchenTypeToReturnDto>>(kitchenTypes);
        return Ok(kitchenTypesToReturn);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetKitchenTypeByIdAsync(int id)
    {
        var kitchenType = await _kitchenServices.GetKitchenTypeByIdAsync(id);
        if (kitchenType == null)
            return NotFound($"Kitchen type with ID {id} not found.");

        var kitchenTypeToReturn = _mapper.Map<KitchenTypeToReturnDto>(kitchenType);
        return Ok(kitchenTypeToReturn);
    }

    [HttpPost]
    public async Task<IActionResult> CreateKitchenTypeAsync(KitchenTypeDto kitchenType)
    {

        if (kitchenType == null)
            return BadRequest("Invalid kitchen type data.");

        var mappedKitchenType = _mapper.Map<KitchenType>(kitchenType);

        var createdKitchenType = await _kitchenServices.CreateKitchenTypeAsync(mappedKitchenType);

        if (createdKitchenType == null)
            return BadRequest(new ApiResponse(404, "Failed to create kitchen type."));

        var kitchenTypeToReturn = _mapper.Map<KitchenTypeToReturnDto>(createdKitchenType);
        return Ok(kitchenTypeToReturn);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateKitchenTypeAsync(int id, KitchenTypeDto kitchenType)
    {
        var kitchenTypeExists = await _kitchenServices.GetKitchenTypeByIdAsync(id);
        if (kitchenTypeExists == null)
        {
            return NotFound($"Kitchen type with ID {id} not found.");
        }
        var mappedKitchenType = _mapper.Map<KitchenType>(kitchenType);
        mappedKitchenType.Id = id; // Ensure the Id from the route is set

        var success = await _kitchenServices.UpdateKitchenTypeAsync(mappedKitchenType);
        if (!success)
        {
            return BadRequest(new ApiResponse(400, "Failed to update kitchen type."));
        }

        var kitchenTypeToReturn = _mapper.Map<KitchenTypeToReturnDto>(mappedKitchenType);

        return Ok(kitchenTypeToReturn);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKitchenTypeAsync(int id)
    {

        var kitchenType = await _kitchenServices.GetKitchenTypeByIdAsync(id);
        if (kitchenType == null)
        {
            return NotFound($"Kitchen type with ID {id} not found.");
        }

        var success = await _kitchenServices.DeleteKitchenTypeAsync(id);
        if (!success)
        {
            return BadRequest("Failed to delete kitchen type.");
        }

        return Ok("Deleted Successfully");
    }
}