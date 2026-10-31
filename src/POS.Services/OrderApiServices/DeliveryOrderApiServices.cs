namespace POS.Services.OrderApiServices;

public class DeliveryOrderApiServices : IDeliveryOrderApiServices
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IPrinterServices _printerServices;
    private readonly IKitchenServices _kitchenServices;
    private readonly string _reportsFolder;

    public DeliveryOrderApiServices(
        IWebHostEnvironment webHostEnvironment,
        IPrinterServices printerServices,
        IKitchenServices kitchenServices)
    {
        _webHostEnvironment = webHostEnvironment;
        _printerServices = printerServices;
        _kitchenServices = kitchenServices;
        _reportsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports");
        Directory.CreateDirectory(_reportsFolder);
    }

    public void BackupDeliveryOrder(OrderDto OrderDto, Orders order)
    {
        order.DeliveryCompany = OrderDto.DeliveryCompany;
        order.TitleName = OrderDto.TitleName;
        order.CustomerID = OrderDto.CustomerID;
        order.CustomerName = OrderDto.CustomerName;
        order.Phone1 = OrderDto.Phone1;
        order.Phone2 = OrderDto.Phone2;
        order.HomeNum = OrderDto.HomeNum;
        order.StreetName = OrderDto.StreetName;
        order.FloorNum = OrderDto.FloorNum;
        order.ApartmentNum = OrderDto.ApartmentNum;
        order.AddressNotice = OrderDto.AddressNotice;
        order.ZoneID = OrderDto.ZoneID;
        order.ZoneName = OrderDto.ZoneName;
        order.ZoneBonus = OrderDto.ZoneBonus;
        order.DispatchID = OrderDto.DispatchID;
        order.DriverID = OrderDto.DriverID;
        order.DriverName = OrderDto.DriverName;
        order.AssignTime = OrderDto.AssignTime;
        order.BackTime = OrderDto.BackTime;
        order.WithoutDeliveryFees = OrderDto.WithoutDeliveryFees;
        order.ClosingTime = OrderDto.ClosingTime;
    }

    public async Task<string> GenerateAndPrintDeliveryReceipts(OrderDto orderDto, Orders createdOrder, List<string> branchDetails, bool isClosed = false)
    {
        var receipt = new Receipt()
        {
            Id = createdOrder?.OrderID ?? orderDto.OrderId,
            StoreName = orderDto.BranchName!,
            CashierName = orderDto.CashierName!,
            ReceiptType = OrderTypes.Delivery.ToString(),
            DateCreated = DateTime.Now,
            PaymentMethod = orderDto.PaymentMethod?.ToString() ?? "N/A",
            FooterMessage = orderDto.FooterMessage ?? "",
            LogoPath = branchDetails[0],
            LogoWidth = int.Parse(branchDetails[1]),
            TotalAmount = orderDto.GrandTotal,
            Discount = orderDto.TotalOrderDiscount,
            Tax = orderDto.Tax,
            Services = orderDto.Services,
            SubTotal = orderDto.SubTotal,
            Items = orderDto.OrderDetails!
        };

        var outputPath = await CreateCashReceiptLayOut(receipt, receipt.Items);
        await PrintCashReceipt(orderDto, outputPath);
        
        return outputPath;
    }

    private async Task<string> CreateCashReceiptLayOut(Receipt receipt, List<TableItem>? receiptItems)
    {
        var document = await Task.Run(() => new ReceiptDocument(receipt, receiptItems!));
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-delivery-receipt.pdf");

        document.GeneratePdf(outputPath);

        return outputPath;
    }

    private async Task PrintCashReceipt(OrderDto orderDto, string reportPath)
    {
        var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
        var printer = kitchens?.ElementAtOrDefault(0);

        var receiptCount = orderDto.OrderSettings!
                .Where(o => o.OrderType == OrderTypes.Delivery.ToString())
                .Select(o => o.CustomerReceiptCount)
                .FirstOrDefault();

        if (printer == null || receiptCount <= 0)
            return;

        var printers = printer!.KitchenPrinters;
        var printerNamesToUse = new List<string>();

        for (int i = 1; i <= receiptCount; i++)
        {
            var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
            if (!string.IsNullOrEmpty(printerName))
            {
                printerNamesToUse.Add(printerName);
            }
        }

        foreach (var printerName in printerNamesToUse)
            await _printerServices.PrintPdfAsync(reportPath, printerName);
    }
}
