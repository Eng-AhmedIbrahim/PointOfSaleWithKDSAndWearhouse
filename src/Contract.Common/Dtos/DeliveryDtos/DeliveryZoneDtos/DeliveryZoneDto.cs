namespace POS.Contract.Dtos.DeliveryDtos.DeliveryZoneDtos;

public class DeliveryZoneDto
{
    public string? ZoneName { get; set; }
    public decimal? DeliveryFee { get; set; }
    public decimal? ZoneBonus { get; set; }
    public int BranchId { get; set; }
}