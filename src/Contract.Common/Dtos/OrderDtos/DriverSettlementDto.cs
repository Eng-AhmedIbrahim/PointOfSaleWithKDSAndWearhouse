namespace POS.Contract.Dtos.OrderDtos;

public class DriverSettlementDto
{
    public string DriverId { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string? DriverDisplayName { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalBonus { get; set; }
    public List<OrderDto>? Orders { get; set; }
}
