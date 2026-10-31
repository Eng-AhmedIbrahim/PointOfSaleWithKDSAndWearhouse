using POS.Contract.Models;

namespace POS.Contract.Models.ReceiptModels;

public record ReceiptItem
{
    public List<TableItem> Items { get; set; }

    public ReceiptItem(List<TableItem> items)
    {
        Items = items;
    }

}
