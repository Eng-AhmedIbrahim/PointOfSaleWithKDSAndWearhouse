using Microsoft.AspNetCore.Mvc;
using POS.Contract.Dtos;
using POS.Core.Services.Contract.ComplaintServices;

namespace POS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComplaintController : ControllerBase
{
    private readonly IComplaintService _complaintService;

    public ComplaintController(IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    [HttpPost]
    public async Task<ActionResult<ComplaintDto>> CreateComplaint(ComplaintDto complaintDto)
    {
        var result = await _complaintService.CreateComplaintAsync(complaintDto);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ComplaintDto>>> GetAllComplaints()
    {
        var result = await _complaintService.GetAllComplaintsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ComplaintDto>> GetComplaintById(int id)
    {
        var result = await _complaintService.GetComplaintByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("phone/{phone}")]
    public async Task<ActionResult<IEnumerable<ComplaintDto>>> GetComplaintsByPhone(string phone)
    {
        var result = await _complaintService.GetComplaintsByPhoneAsync(phone);
        return Ok(result);
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var result = await _complaintService.UpdateComplaintStatusAsync(id, status);
        if (!result) return NotFound();
        return NoContent();
    }
}
