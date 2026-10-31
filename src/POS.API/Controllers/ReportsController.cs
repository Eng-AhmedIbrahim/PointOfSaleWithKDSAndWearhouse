using Microsoft.AspNetCore.Mvc;
using POS.Contract.Dtos.ReportingDtos;
using POS.Reports;

namespace POS.API.Controllers;

public class ReportsController : BaseApiController
{
    private readonly IReportsManager _reportsManager;

    public ReportsController(IReportsManager reportsManager)
    {
        _reportsManager = reportsManager;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ReportResponseDto>> GenerateReport([FromBody] ReportRequestDto request)
    {
        var response = await _reportsManager.GenerateReport(request);
        return Ok(response);
    }
}
