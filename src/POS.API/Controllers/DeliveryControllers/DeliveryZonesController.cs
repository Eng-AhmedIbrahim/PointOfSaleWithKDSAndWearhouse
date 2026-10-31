namespace POS.API.Controllers.DeliveryControllers;

public class DeliveryZonesController : BaseApiController
{
    private readonly IDeliveryZoneServices _deliveryZoneServices;
    private readonly IMapper _mapper;

    public DeliveryZonesController(IDeliveryZoneServices deliveryZoneServices,IMapper mapper)
    {
        _deliveryZoneServices = deliveryZoneServices;
        _mapper = mapper;
    }

    [HttpGet("Get-All-Zones")]
    public async Task<IActionResult> GetAllZones()
    {
        var zones = await _deliveryZoneServices.GetAllZonesAsync();
        var zonesToReturn = _mapper.Map<IEnumerable<DeliveryZone>, IEnumerable<DeliveryZonesToReturnDto>>(zones);
        return Ok(zonesToReturn);
    }

    [HttpGet("branch/{branchId}")]
    public async Task<IActionResult> GetZonesByBranch(int branchId)
    {
        var zones = await _deliveryZoneServices.GetZonesByBranchAsync(branchId);
        var zonesToReturn = _mapper.Map<IEnumerable<DeliveryZone>, IEnumerable<DeliveryZonesToReturnDto>>(zones);

        return Ok(zonesToReturn);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetZoneById(int id)
    {
        var zone = await _deliveryZoneServices.GetZoneByIdAsync(id);
        if(zone is null)
            return NotFound(new ApiResponse(404,"Zone not found"));

        var zoneToReturn = _mapper.Map<DeliveryZone, DeliveryZonesToReturnDto>(zone);

        return Ok(zoneToReturn);
    }

    [HttpPost("Create-Zone")]
    public async Task<IActionResult> CreateZone(DeliveryZoneDto zone)
    {
        if (zone == null)
            return BadRequest(new ApiResponse(400,"Zone is null"));

        var zoneToCreate = _mapper.Map<DeliveryZoneDto, DeliveryZone>(zone);
        
        var createdZone = await _deliveryZoneServices.CreateZoneAsync(zoneToCreate);
        if(createdZone is null)
            return BadRequest(new ApiResponse(400,"Zone Cannot be created"));

        var createdZoneToReturn = _mapper.Map<DeliveryZone, DeliveryZonesToReturnDto>(createdZone);
        return Ok(createdZoneToReturn);
    }

    [HttpPut("update-zone/{id}")]
    public async Task<IActionResult> UpdateZone(int id, DeliveryZone zone)
    {
        if (id != zone.Id)
        {
            return BadRequest("Zone ID mismatch.");
        }

        var updated = await _deliveryZoneServices.UpdateZoneAsync(zone);
        if (!updated) return NotFound();

        return NoContent();
    }

    [HttpDelete("/delete-zone/{id}")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        var deleted = await _deliveryZoneServices.DeleteZoneAsync(id);
        if (!deleted) return NotFound();

        return NoContent();
    }
}