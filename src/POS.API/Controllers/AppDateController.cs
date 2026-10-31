namespace POS.API.Controllers;

public class AppDateController : BaseApiController
{
    private readonly IAppDateService _appSettingService;
    private readonly IMapper _mapper;

    public AppDateController(IAppDateService appSettingService,
        IMapper mapper)
    {
        _appSettingService = appSettingService;
        _mapper = mapper;
    }

    [HttpGet("get-app-date")]
    public async Task<ActionResult<AppDateToReturnDto>> GetAppDate()
    {
        var appDate = await _appSettingService.GetAppDateAsync();
        var mappedAppDate = _mapper.Map<AppDate, AppDateToReturnDto>(appDate);
        return Ok(mappedAppDate);
    }

    [HttpPut("update-app-date")] 
    public async Task<ActionResult<AppDateToReturnDto>> UpdateAppDate()
    {
        var appDate = await _appSettingService.UpdateAppDate();
        var mappedAppDate = _mapper.Map<AppDate, AppDateToReturnDto>(appDate);

        return Ok(mappedAppDate);
    }

    [HttpPut("update-order-numbers")]
    public async Task<ActionResult<AppDateToReturnDto>> UpdateCurrentOrderNumber()
    {
        var appDate = await _appSettingService.UpdateOrderNumber();
        var mapped = _mapper.Map<AppDate, AppDateToReturnDto>(appDate);
        return Ok(mapped);
    }

    [HttpPut("close-day")]
    public async Task<ActionResult<bool>> CloseDay()
    {
        var result = await _appSettingService.CloseDayAsync();
        return Ok(result);
    }
}