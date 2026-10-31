using POS.Contract.Models.ReceiptModels;

public static class FakeDataGenerator
{
    private static readonly Random Random = new();

    public static (Receipt receipt, List<TableItem> tableItems) GenerateFakeReceipt(int itemCount = 10)
    {
        var receipt = new Receipt
        {
            Id = 1,
            StoreName = "Test Store",
            CashierName = "Administrator",
            ReceiptType = "تيك اواي",
            PaymentMethod = "كاش",
            FooterMessage = "علموا اولادكم ان فلسطين عربية\nFor Delivery Call: +1234567890",
            DateCreated = DateTime.Now
        };

        var items = GenerateFakeItems(itemCount);
        receipt.AddItems(items); // Optional if needed elsewhere

        // Flattened TableItems list
        var allTableItems = items.SelectMany(ri => ri.Items).ToList();

        return (receipt, allTableItems);
    }

    private static List<ReceiptItem> GenerateFakeItems(int count)
    {
        var receiptItems = new List<ReceiptItem>();

        for (int i = 1; i <= count; i++)
        {
            var tableItems = new List<TableItem>();

            int tableItemCount = Random.Next(1, 3); // Each ReceiptItem may have 1 or 2 TableItems
            for (int j = 1; j <= tableItemCount; j++)
            {
                var quantity = Random.Next(1, 5);
                var price = (decimal)(Random.NextDouble() * 50 + 5);
                var total = quantity * price;
                var lineDiscount = Random.NextDouble() < 0.3 ? (decimal?)Random.Next(1, 10) : null;

                var item = new TableItem
                {
                    Id = i * 10 + j,
                    Name = $"عنصر {i}-{j}",
                    Quantity = quantity,
                    Price = price,
                    Total = total,
                    LineComment = Random.NextDouble() < 0.5 ? "تعليق تجريبي" : null,
                    IsReadOnly = false,
                    Attributes = Random.NextDouble() < 0.6 ? GenerateFakeAttributes(Random.Next(1, 4)) : new List<AttributeDto>()
                };

                tableItems.Add(item);
            }

            receiptItems.Add(new ReceiptItem(tableItems));
        }

        return receiptItems;
    }

    private static List<AttributeDto> GenerateFakeAttributes(int count)
    {
        var attributes = new List<AttributeDto>();
        for (int i = 1; i <= count; i++)
        {
            attributes.Add(new AttributeDto
            {
                Id = i,
                Name = $"إضافة {i}"
            });
        }
        return attributes;
    }
}
