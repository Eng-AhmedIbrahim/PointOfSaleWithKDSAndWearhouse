using System;

namespace POS.Contract.Dtos.OrderDtos;

public class OrderTrackDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string? OrderType { get; set; }
    public string? Action { get; set; }
    public string? UserName { get; set; }
    public string? UserId { get; set; }
    public string? MachineName { get; set; }
    public string? Details { get; set; }
    public DateTime ActionDateTime { get; set; }
    public int? TableId { get; set; }
    public string? TableName { get; set; }
}
