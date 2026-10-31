namespace POS.API.Controllers.DeliveryControllers;

public class DeliveryCustomerTitleController : BaseApiController
{
    private readonly IDeliveryCustomerTitleService _titleService;

    public DeliveryCustomerTitleController(IDeliveryCustomerTitleService titleService)
    {
        _titleService = titleService;
    }

    [HttpGet("Get-All-Titles")]
    public async Task<IActionResult> GetAll()
    {
        var titles = await _titleService.GetAllAsync();
        return Ok(titles);
    }

    [HttpGet("Get-Title-By-Id/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var title = await _titleService.GetByIdAsync(id);
        if (title == null)
            return NotFound();

        return Ok(title);
    }

    [HttpPost("Create-Title")]
    public async Task<IActionResult> Create([FromBody] DeliveryTitleDto titleDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _titleService.AddAsync(titleDto);
        return Ok(titleDto);
    }

    [HttpPut("Update-Title/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DeliveryCustomerTitle titleDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var exists = await _titleService.ExistsAsync(id);
        if (!exists)
            return NotFound();

        await _titleService.UpdateAsync(titleDto);
        return NoContent();
    }

    [HttpDelete("Delete-Title/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var exists = await _titleService.ExistsAsync(id);
        if (!exists)
            return NotFound();

        await _titleService.DeleteAsync(id);
        return NoContent();
    }
}