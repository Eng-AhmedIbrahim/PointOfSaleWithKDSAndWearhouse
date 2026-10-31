namespace POS.API.Controllers.KitchenControllers;

public class KitchenPrintersController : BaseApiController
{
    private readonly IKitchenPrintersService _kitchenPrintersService;
    private readonly IMapper _mapper;

    public KitchenPrintersController(IKitchenPrintersService kitchenPrintersService, IMapper mapper)
    {
        _kitchenPrintersService = kitchenPrintersService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllKitchenPrintersAsync()
    {
        var kitchenPrinters = await _kitchenPrintersService.GetAllPrintersAsync();
        if (kitchenPrinters == null || !kitchenPrinters.Any())
        {
            return Ok(new List<KitchenPrintersToReturnDto>());
        }

        var kitchenPrintersToReturn = _mapper.Map<ICollection<KitchenPrintersToReturnDto>>(kitchenPrinters);
        return Ok(kitchenPrintersToReturn);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<KitchenPrintersToReturnDto>> GetKitchenPrinterByIdAsync(int id)
    {
        var kitchenPrinter = await _kitchenPrintersService.GetPrinterByIdAsync(id);
        if (kitchenPrinter == null)
        {
            return NotFound($"Kitchen printer with ID {id} not found.");
        }

        var kitchenPrinterToReturn = _mapper.Map<KitchenPrintersToReturnDto>(kitchenPrinter);
        return Ok(kitchenPrinterToReturn);
    }

    [HttpPost]
    public async Task<ActionResult<KitchenPrintersToReturnDto>> CreateKitchenPrinterAsync(KitchenPrintersDto kitchenPrinter)
    {
        if (kitchenPrinter == null)
        {
            return BadRequest("Invalid kitchen printer data.");
        }
        var mappedKitchenPrinter = _mapper.Map<KitchenPrinters>(kitchenPrinter);

        var createdKitchenPrinter = await _kitchenPrintersService.CreatePrinterAsync(mappedKitchenPrinter);

        if (createdKitchenPrinter == null)
        {
            return BadRequest("Failed to create kitchen printer.");
        }

        var kitchenPrinterToReturn = _mapper.Map<KitchenPrintersToReturnDto>(createdKitchenPrinter);
        return Ok(kitchenPrinterToReturn);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateKitchenPrinterAsync(int id, KitchenPrintersDto kitchenPrinterDto)
    {
        var kitchenPrinterExists = await _kitchenPrintersService.GetPrinterByIdAsync(id);
        if (kitchenPrinterExists == null)
        {
            return NotFound($"Kitchen printer with ID {id} not found.");
        }

        var mappedPrinter = _mapper.Map<KitchenPrinters>(kitchenPrinterDto);
        mappedPrinter.Id = id;

        var success = await _kitchenPrintersService.UpdatePrinterAsync(kitchenPrinterExists, mappedPrinter);
        if (success is null)
        {
            return BadRequest(new ApiResponse(400, "Failed to update kitchen printer."));
        }

        return Ok(success);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteKitchenPrinterAsync(int id)
    {
        var kitchenPrinter = await _kitchenPrintersService.GetPrinterByIdAsync(id);
        if (kitchenPrinter == null)
        {
            return NotFound($"Kitchen printer with ID {id} not found.");
        }

        var success = await _kitchenPrintersService.DeletePrinter(kitchenPrinter);
        if (!success)
        {
            return BadRequest("Failed to delete kitchen printer.");
        }

        return Ok("Deleted Successfully");
    }
}