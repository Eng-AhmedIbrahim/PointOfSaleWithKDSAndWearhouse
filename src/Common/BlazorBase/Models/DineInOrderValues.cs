namespace BlazorBase.Models;

public class DineInOrderValues
{
    public int OrderID { get; set; }
    public string? TableOpenTime { get; set; }
    public string? TableName { get; set; }
    public string? CaptainName { get; set; }
    public decimal? Total { get; set; }
    public decimal? Discount { get; set; }
}