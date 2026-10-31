namespace BlazorBase.Models;

public class DeliverySettings
{
    public string? OrderStatment { get; set; }
    public decimal? Service { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Tips { get; set; }
    public int? JobID { get; set; }
    public int? CustomerReceiptCount { get; set; }
    public int? FullKitchenReceiptCount { get; set; }
    public int? SeparateReceiptCount { get; set; }
    public int? ClosingReceiptCount { get; set; }
    public bool? AddServiceToItemPrice { get; set; }
}
