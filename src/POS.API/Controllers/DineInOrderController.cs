using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using POS.Core.Entities.DineIn;
using POS.Core.Entities.OrderEntity;
using POS.Core.Services.Contract.DineInOrderServices;
using POS.Contract.Dtos.DineIn;

namespace POS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DineInOrderController : ControllerBase
{
    private readonly IDineInOrderService _dineInOrderService;
    private readonly IMapper _mapper;
    private readonly ILogger<DineInOrderController> _logger;

    public DineInOrderController(
        IDineInOrderService dineInOrderService,
        IMapper mapper,
        ILogger<DineInOrderController> logger)
    {
        _dineInOrderService = dineInOrderService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<DineInOrderDto>> CreateDineInOrder([FromBody] DineInOrderDto orderDto)
    {
        try
        {
            var order = _mapper.Map<Orders>(orderDto);
            var result = await _dineInOrderService.CreateDineInOrderAsync(order);
            if (result is null)
                return BadRequest("Failed to create DineIn order");

            return Ok(_mapper.Map<DineInOrderDto>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating DineIn order");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut]
    public async Task<ActionResult<DineInOrderDto>> UpdateDineInOrder([FromBody] DineInOrderDto orderDto)
    {
        try
        {
            var order = _mapper.Map<Orders>(orderDto);
            var result = await _dineInOrderService.UpdateDineInOrderAsync(order);
            if (result is null)
                return NotFound("DineIn order not found");

            return Ok(_mapper.Map<DineInOrderDto>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DineIn order");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<DineInOrderDto>> GetDineInOrderById(int orderId)
    {
        try
        {
            var result = await _dineInOrderService.GetDineInOrderByIdAsync(orderId);
            if (result is null)
                return NotFound($"No order found with ID {orderId}");

            return Ok(_mapper.Map<DineInOrderDto>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DineIn order by ID");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("table/{tableId}")]
    public async Task<ActionResult<DineInOrderDto>> GetDineInOrderByTableId(int tableId, [FromQuery] string state = "Open")
    {
        try
        {
            var result = await _dineInOrderService.GetDineInOrderByTableIdAsync(tableId, state);
            if (result is null)
                return NotFound($"No {state} order found for table {tableId}");

            return Ok(_mapper.Map<DineInOrderDto>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DineIn order by table ID");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("open-orders/{tableId}")]
    public async Task<ActionResult<IReadOnlyList<DineInOrderDto>>> GetOpenOrdersByTableId(int tableId)
    {
        try
        {
            var result = await _dineInOrderService.GetOpenOrdersByTableIdAsync(tableId);
            return Ok(_mapper.Map<IReadOnlyList<DineInOrderDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting open DineIn orders by table ID");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("open")]
    public async Task<ActionResult<IReadOnlyList<DineInOrderDto>>> GetAllOpenDineInOrders()
    {
        try
        {
            var result = await _dineInOrderService.GetAllOpenDineInOrdersAsync();
            return Ok(_mapper.Map<IReadOnlyList<DineInOrderDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all open DineIn orders");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("close/{orderId}")]
    public async Task<ActionResult<bool>> CloseDineInOrder(int orderId, [FromQuery] decimal? paid = null, [FromQuery] decimal? remain = null)
    {
        try
        {
            var result = await _dineInOrderService.CloseDineInOrderAsync(orderId, paid, remain);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing DineIn order");
            return StatusCode(500, "Internal server error");
        }
    }


    [HttpPost("{dineInOrderId}/items")]
    public async Task<ActionResult<bool>> AddItemsToDineInOrder(int dineInOrderId, [FromBody] List<OrderItemsDetailsDto> itemsDto)
    {
        try
        {
            var items = _mapper.Map<List<OrderItemsDetails>>(itemsDto);
            var result = await _dineInOrderService.AddItemsToDineInOrderAsync(dineInOrderId, items);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding items to DineIn order");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{dineInOrderId}/discount")]
    public async Task<ActionResult<bool>> UpdateDineInOrderDiscount(
        int dineInOrderId,
        [FromBody] UpdateDiscountRequest request)
    {
        try
        {
            var result = await _dineInOrderService.UpdateDineInOrderDiscountAsync(
                dineInOrderId,
                request.DiscountAmount,
                request.DiscountPercentage,
                request.DiscountType,
                request.DiscountReason);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DineIn order discount");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("transfer/{orderId}/{newTableId}/{newTableName}")]
    public async Task<ActionResult<bool>> TransferDineInOrder(int orderId, int newTableId, string newTableName)
    {
        try
        {
            var result = await _dineInOrderService.TransferDineInOrderAsync(orderId, newTableId, newTableName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring DineIn order");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("merge")]
    public async Task<ActionResult<bool>> MergeDineInOrders([FromBody] MergeOrdersRequest request)
    {
        try
        {
            var result = await _dineInOrderService.MergeDineInOrdersAsync(request.PrimaryOrderId, request.SecondaryOrderIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging DineIn orders");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("split")]
    public async Task<IActionResult> SplitDineInOrder([FromBody] SplitOrderRequest request)
    {
        var result = await _dineInOrderService.SplitDineInOrderAsync(request.SourceOrderId, request.Targets);
        if (result)
            return Ok(new { Message = "Order split successfully" });
        return BadRequest(new { Message = "Failed to split order" });
    }


    
    [HttpPost("{orderId}/increment-print")]
    public async Task<ActionResult<int>> IncrementPrintCount(int orderId)
    {
        try
        {
            var result = await _dineInOrderService.IncrementPrintCountAsync(orderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing print count for order {OrderId}", orderId);
            return StatusCode(500, "Internal server error");
        }
    }
    
    [HttpPost("reserve")]
    public async Task<ActionResult<bool>> ReserveTable([FromBody] DineInOrderDto reservationDto)
    {
        try
        {
            var reservationOrder = _mapper.Map<Orders>(reservationDto);
            var result = await _dineInOrderService.ReserveTableAsync(reservationOrder);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving table");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("cancel-reservation/{orderId}")]
    public async Task<ActionResult<bool>> CancelReservation(int orderId)
    {
        try
        {
            var result = await _dineInOrderService.CancelReservationAsync(orderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling reservation for order {OrderId}", orderId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("seat-reservation/{orderId}")]
    public async Task<ActionResult<bool>> SeatReservation(int orderId, [FromBody] SeatReservationRequest request)
    {
        try
        {
            var result = await _dineInOrderService.SeatReservationAsync(orderId, request.CaptainId, request.CaptainName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seating reservation for order {OrderId}", orderId);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class SeatReservationRequest
{
    public string CaptainId { get; set; } = string.Empty;
    public string CaptainName { get; set; } = string.Empty;
}

public class VoidOrderRequest
{
    public int OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string VoidBy { get; set; } = string.Empty;
    public string VoidByName { get; set; } = string.Empty;
}

public class VoidItemsRequest
{
    public int OrderId { get; set; }
    public List<OrderItemVoidDto> ItemsToVoid { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public string VoidBy { get; set; } = string.Empty;
    public string VoidByName { get; set; } = string.Empty;
}

public class SplitOrderRequest
{
    public int SourceOrderId { get; set; }
    public List<SplitTargetDto> Targets { get; set; } = new();
}

public class MergeOrdersRequest
{
    public int PrimaryOrderId { get; set; }
    public List<int> SecondaryOrderIds { get; set; } = new();
}

public class UpdateDiscountRequest
{
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountType { get; set; }
    public string? DiscountReason { get; set; }
}
