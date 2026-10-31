using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using POS.Core.Entities.OrderEntity;
using POS.Core.Services.Contract.OrderTrackServices;
using POS.Contract.Dtos.OrderDtos;

namespace POS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderTrackController : ControllerBase
{
    private readonly IOrderTrackService _orderTrackService;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderTrackController> _logger;

    public OrderTrackController(
        IOrderTrackService orderTrackService,
        IMapper mapper,
        ILogger<OrderTrackController> logger)
    {
        _orderTrackService = orderTrackService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<bool>> TrackOrderAction([FromBody] OrderTrackDto orderTrackDto)
    {
        try
        {
            var orderTrack = _mapper.Map<OrderTrack>(orderTrackDto);
            var result = await _orderTrackService.TrackOrderActionAsync(orderTrack);
            if (!result)
                return BadRequest("Failed to track order action");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking order action");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<IReadOnlyList<OrderTrackDto>>> GetOrderTrackingHistory(int orderId)
    {
        try
        {
            var result = await _orderTrackService.GetOrderTrackingHistoryAsync(orderId);
            return Ok(_mapper.Map<IReadOnlyList<OrderTrackDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order tracking history");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("daterange")]
    public async Task<ActionResult<IReadOnlyList<OrderTrackDto>>> GetOrderTrackingByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var result = await _orderTrackService.GetOrderTrackingByDateRangeAsync(startDate, endDate);
            return Ok(_mapper.Map<IReadOnlyList<OrderTrackDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order tracking by date range");
            return StatusCode(500, "Internal server error");
        }
    }
}
