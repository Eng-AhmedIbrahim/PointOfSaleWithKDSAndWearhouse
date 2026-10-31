namespace POS.Contract.Models;
public class OrderDetails
{
    public List<TableItem>? Items { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Discount { get; set; }
    public decimal TotalAmount { get; set; }
}