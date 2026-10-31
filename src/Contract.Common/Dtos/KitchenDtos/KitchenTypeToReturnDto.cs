namespace POS.Contract.Dtos.KitchenDtos;

public class KitchenTypeToReturnDto
{
    public int Id { get; set; }
    public string? KitchenName { get; set; }
    public int? KitchenPrinterId { get; set; }
    public KitchenPrintersDto? KitchenPrinter { get; set; }
}