namespace POS.Contract.Models;

public class TableItemDetails
{
    public TableItem? TableItem { get; set; }
    public bool HasDiscount { get; set; }
    public decimal? DiscountPercentage { get; set; } = null;
    public decimal? DiscountAmount { get; set; } = null;
    public bool HasTax { get; set; } = false;
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public TableItemDetails Clone()
    {
        return new TableItemDetails
        {
            TableItem = this.TableItem?.Clone(), // Clone TableItem if it's not null
            HasDiscount = this.HasDiscount,
            DiscountPercentage = this.DiscountPercentage,
            DiscountAmount = this.DiscountAmount,
            HasTax = this.HasTax,
            TaxAmount = this.TaxAmount,
            TotalAmount = this.TotalAmount
        };
    }
}