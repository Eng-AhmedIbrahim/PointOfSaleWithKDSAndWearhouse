namespace BackOffice.Desktop.Services;

public class DesktopPrintOrderService : IPrintOrderService
{
    private readonly CommonProperties _commonProperties;
    private readonly IOrderSettingsService _orderSettingsService;
    private readonly IPrinterServices _printerServices;
    private readonly IBranchService _branchService;

    private string LogoPath
    {
        get
        {
            try
            {
                var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "ReceiptLogo");
                if (!Directory.Exists(folderPath))
                    folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "ReceiptLogo");
                
                if (Directory.Exists(folderPath))
                {
                    var file = Directory.GetFiles(folderPath, "*.*")
                        .FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                            f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
                    return file ?? "";
                }
            }
            catch { }
            return "";
        }
    }

    private readonly ILocalStorageService _localStorage;

    public DesktopPrintOrderService(
        CommonProperties commonProperties,
        IOrderSettingsService orderSettingsService,
        IPrinterServices printerServices,
        IBranchService branchService,
        IConfiguration configuration,
        ILocalStorageService localStorage)
    {
        _commonProperties = commonProperties;
        _orderSettingsService = orderSettingsService;
        _printerServices = printerServices;
        _branchService = branchService;
        _localStorage = localStorage;
    }

    public async Task PrintDineInClosingReceipt(DineInOrderDetails order)
    {
        await PrintInitialDineInOrder(order, isClosing: true);
    }


    public async Task PrintInitialDineInOrder(DineInOrderDetails order, 
        bool printCustomer = true, 
        bool printKitchen = true, 
        bool isClosing = false,
        bool isUpdate = false,
        bool isPrePrint = false,
        string? printerName = null,
        bool isCopy = false)
    {
        var branch = await _branchService.GetBranches();
        var currentBranch = branch.FirstOrDefault(b => b.Id == _commonProperties.BranchDetails?.Id);

        var receipt = new DineInReceipt()
        {
            Id = order.BasicOrderDetails!.OrderId,
            StoreName = currentBranch?.Name ?? "Store",
            CashierName = order.BasicOrderDetails.CashierName ?? "Cashier",
            CaptainName = order.CaptainName ?? "Captain",
            ReceiptType = "DineIn",
            DateCreated = order.BasicOrderDetails?.OrderDataTime ?? DateTime.Now,
            PaymentMethod = "Cash",
            FooterMessage = _commonProperties.DineInSettings?.OrderStatment ?? "",
            LogoPath = LogoPath, 
            LogoWidth = currentBranch?.LogoWidth ?? 150,
            TotalAmount = order.BasicOrderDetails!.Account,
            ServiceAmount = order.BasicOrderDetails.Service?.ToString("0.##") ?? "0.00",
            TaxAmount = order.BasicOrderDetails.Tax?.ToString("0.##") ?? "0.00",
            Discount = (order.BasicOrderDetails.Items?.Sum(i => i.TotalDiscountPrice ?? 0) ?? 0) + 
                       (order.BasicOrderDetails.OrderDiscount?.Value ?? 0),
            TotalOrder = order.BasicOrderDetails.Total?.ToString("0.##") ?? "0.00",
            IsHospitality = order.BasicOrderDetails.IsHospitality,
            HospitalityResponsibleName = order.BasicOrderDetails.HospitalityResponsibleName,
            HospitalityReason = order.BasicOrderDetails.HospitalityReason,
            IsStaffMeal = order.BasicOrderDetails.IsStaffMeal,
            StaffMealEmployeeName = order.BasicOrderDetails.StaffMealEmployeeName
        };

        var items = order.BasicOrderDetails.Items ?? new List<TableItem>();
        var orderSettings = _commonProperties.OrderSettings?.FirstOrDefault(o => o.OrderType == "DineIn");
        
        if (printCustomer)
        {
            int countToPrint = 0;
            if (orderSettings != null)
            {
                 countToPrint = isClosing 
                     ? (orderSettings.ClosingReceiptCount ?? 0) 
                     : (orderSettings.CustomerReceiptCount ?? 0);
            }

            Log.Information("PrintDineInLocally started. isClosing: {isClosing}, countToPrint: {countToPrint}", isClosing, countToPrint);

            int currentPrintCount = 0;
            if (!isUpdate)
            {
                try
                {
                    currentPrintCount = await _orderSettingsService.IncrementPrintCountAsync(order.DatabaseId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error incrementing print count for DineIn order {DatabaseId}", order.DatabaseId);
                }
            }

            if (countToPrint > 0)
            {
                var kitchens = await _printerServices.GetKitchenTypesAsync();
                Log.Information("Fetched {Count} kitchens", kitchens?.Count ?? 0);
                
                var customerKitchen = kitchens?.ElementAtOrDefault(0);
                if (customerKitchen == null) Log.Warning("customerKitchen (Element 0) is null");
                
                if (customerKitchen != null)
                {
                    var printers = customerKitchen.KitchenPrinters?.FirstOrDefault();
                    if (printers == null) Log.Warning("printers (KitchenPrinters) is null for kitchen {KitchenName}", customerKitchen.KitchenName);

                    if (printers != null)
                    {
                        string? originalPath = null;
                        string? copyPath = null;
                        var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                        Directory.CreateDirectory(reportsFolder);

                        for (int i = 1; i <= countToPrint; i++)
                        {
                            var currentPrinterName = printerName ?? typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                            Log.Information("Iteration {i}: printerName resolved to '{currentPrinterName}'", i, currentPrinterName);
                            
                            if (string.IsNullOrEmpty(currentPrinterName)) continue;

                            bool isActuallyCopy = (currentPrintCount > 1) || (i > 1);

                            string pathToPrint;
                            if (isActuallyCopy)
                            {
                                if (copyPath == null)
                                {
                                    receipt.IsCopy = true;
                                    var document = new DineInReceiptDocument(receipt, items);
                                    copyPath = Path.Combine(reportsFolder, $"Order_{order.BasicOrderDetails!.OrderId}_DineIn_Copy_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                    Log.Information("Generating COPY PDF: {Path}", copyPath);
                                    document.GeneratePdf(copyPath);
                                    if (!File.Exists(copyPath)) Log.Error("Failed to generate COPY PDF at {Path}", copyPath);
                                }
                                pathToPrint = copyPath;
                            }
                            else
                            {
                                if (originalPath == null)
                                {
                                    receipt.IsCopy = false;
                                    var document = new DineInReceiptDocument(receipt, items);
                                    originalPath = Path.Combine(reportsFolder, $"Order_{order.BasicOrderDetails!.OrderId}_DineIn_Original_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                    Log.Information("Generating ORIGINAL PDF: {Path}", originalPath);
                                    document.GeneratePdf(originalPath);
                                    if (!File.Exists(originalPath)) Log.Error("Failed to generate ORIGINAL PDF at {Path}", originalPath);
                                }
                                pathToPrint = originalPath;
                            }

                            try
                            {
                                Log.Information("Sending PDF to printer '{PrinterName}': {Path}", currentPrinterName, pathToPrint);
                                var printSuccess = await _printerServices.PrintPdfAsync(pathToPrint, currentPrinterName);
                                Log.Information("PrintPdfAsync result: {Success} for printer {PrinterName}", printSuccess, currentPrinterName);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error printing to printer {PrinterName}", currentPrinterName);
                            }
                        }
                    }
                }
            }
        }

        if (printKitchen && orderSettings != null)
        {
             var orderDto = new OrderDto
             {
                 Id = order.DatabaseId,
                 OrderId = order.BasicOrderDetails!.OrderId,
                 CashierName = order.BasicOrderDetails.CashierName,
                 OrderType = "DineIn",
                 OrderDetails = order.BasicOrderDetails.Items,
                 TableName = order.RelatedTableName,
                 OrderNotice = _commonProperties.OrderNote,
                 OrderSettings = _commonProperties.OrderSettings,
                 DiscountReason = order.BasicOrderDetails.OrderDiscount?.DiscountReason,
                 DiscountType = order.BasicOrderDetails.OrderDiscount?.DiscountType,
                 DiscountPercentage = order.BasicOrderDetails.OrderDiscount?.Percentage ?? 0,
                 TotalOrderDiscount = order.BasicOrderDetails.OrderDiscount?.Value ?? 0
             };

             if (orderSettings.FullKitchenReceiptCount > 0)
                 await PrintBackupReceiptsLocally(orderDto, orderDto, isReprint: isClosing, isCopy: isCopy);

             if (orderSettings.SeparateReceiptCount > 0)
                 await PrintKitchenReceiptsLocally(orderDto, orderDto, isReprint: isClosing, isCopy: isCopy);
        }
    }

    public async Task<bool> PrintTakeAwayOrder(decimal paid = 0,
        string customerName = "", 
        string customerPhone = "", 
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        BackupMainOrderDtoDetails(customerName, customerPhone, paid, paymentMethod);

        _commonProperties.OrderDto!.SkipPrintingOnServer = true;

        // Guard: لا ترسل طلب فاضي للـ API
        if (_commonProperties.OrderDto.OrderDetails == null || !_commonProperties.OrderDto.OrderDetails.Any())
        {
            Log.Warning("PrintTakeAwayOrder: OrderDetails is empty! TableItems count={Count}. Cannot create order.",
                _commonProperties.TableItems?.Count ?? 0);
            return false;
        }

        var result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);

        if (result is null)
            return false;

        _commonProperties.CurrentOrderId = result.OrderId + 1;
        if (result.OrderDate.HasValue)
            _commonProperties.PosDate = DateOnly.FromDateTime(result.OrderDate.Value);
        
        try
        {
            await PrintTakeAwayLocally(_commonProperties.OrderDto!, result);

            var orderSettings = _commonProperties.OrderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "TakeAway");
            if (orderSettings != null)
            {
                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(_commonProperties.OrderDto!, result, isReprint: true);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(_commonProperties.OrderDto!, result, isReprint: true);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print takeaway order locally");
        }

        return true;
    }

    public async Task<bool> ReprintOrderAsync(int orderId, bool isCopy = false, bool printCustomer = true, bool printKitchen = true, string? printerName = null)
    {
        var order = await _orderSettingsService.GetOrderByIdAsync(orderId);
        if (order == null) return false;

        if (order.OrderSettings == null || !order.OrderSettings.Any())
        {
             order.OrderSettings = _commonProperties.OrderSettings;
        }

        if (order.OrderType == "DineIn")
        {
            var dineInOrderDetails = new DineInOrderDetails
            {
                BasicOrderDetails = new BlazorBase.Models.OrderDetails
                {
                    OrderId = order.OrderId,
                    CashierName = order.CashierName,
                    Items = order.OrderDetails ?? new List<TableItem>(),
                    OrderDataTime = order.OrderDate ?? DateTime.Now,
                    Total = order.GrandTotal ?? 0,
                    Service = order.Services ?? 0,
                    Tax = order.Tax ?? 0,
                    Account = order.SubTotal ?? 0,
                    OrderDiscount = new OrderDiscount
                    {
                        Value = order.TotalDiscount ?? 0, 
                        Percentage = order.DiscountPercentage ?? 0
                    }
                },
                CaptainName = order.WaiterName ?? "Unknown", 
                RelatedTableId = order.TableId,
                RelatedTableName = order.TableName,
                DatabaseId = order.Id
            };
            
            await PrintInitialDineInOrder(dineInOrderDetails, printCustomer: printCustomer, printKitchen: printKitchen, isClosing: false, isUpdate: true, isPrePrint: false, printerName: printerName, isCopy: isCopy);
        }
        else if (order.OrderType == "Delivery")
        {
            if (printCustomer)
                await PrintDeliveryLocally(order, order, isFollowUp: false, parentDeliveryFees: null, isCopy: isCopy, printerName: printerName);
            
            var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");
            if (orderSettings != null && printKitchen)
            {
                // Force at least 1 in reprint if not specified
                var rCount = orderSettings.FullKitchenReceiptCount > 0 ? orderSettings.FullKitchenReceiptCount : 1;
                await PrintBackupReceiptsLocally(order, order, isReprint: true, reprintCount: rCount, isCopy: isCopy, targetPrinterName: printerName);

                var sCount = orderSettings.SeparateReceiptCount > 0 ? orderSettings.SeparateReceiptCount : 1;
                await PrintKitchenReceiptsLocally(order, order, isReprint: true, reprintCount: sCount, isCopy: isCopy, targetPrinterName: printerName);
            }
        }
        else 
        {
            if (printCustomer)
                await PrintTakeAwayLocally(order, order, isCopy: isCopy, printerName: printerName);
                
            var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == "TakeAway");
            if (orderSettings != null && printKitchen)
            {
                var rCount = orderSettings.FullKitchenReceiptCount > 0 ? orderSettings.FullKitchenReceiptCount : 1;
                await PrintBackupReceiptsLocally(order, order, isReprint: true, reprintCount: rCount, isCopy: isCopy, targetPrinterName: printerName);

                var sCount = orderSettings.SeparateReceiptCount > 0 ? orderSettings.SeparateReceiptCount : 1;
                await PrintKitchenReceiptsLocally(order, order, isReprint: true, reprintCount: sCount, isCopy: isCopy, targetPrinterName: printerName);
            }
        }

        return true;
    }

    private void BackupMainOrderDtoDetails(string customerName, 
        string customerPhone, 
        decimal paid = 0, 
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        _commonProperties.OrderDto!.OrderId = _commonProperties.CurrentOrderId;
        _commonProperties.OrderDto!.OrderType = "TakeAway";
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.BranchId = _commonProperties.BranchDetails!.Id;
        _commonProperties.OrderDto.BranchName = _commonProperties.BranchDetails.Name;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.FooterMessage = _commonProperties.TakeAwaySettings!.OrderStatment;
        _commonProperties.OrderDto.PaymentMethod = paymentMethod;
        _commonProperties.OrderDto.Paid = paid > 0.00m ? paid : (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0);
        _commonProperties.OrderDto.SubTotal = _commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 0 ? _commonProperties._financeSettingsList[0].Value : 0;
        _commonProperties.OrderDto.Services = _commonProperties.TakeAwaySettings!.Service;
        _commonProperties.OrderDto.Tax = _commonProperties.TakeAwaySettings!.Tax;
        _commonProperties.OrderDto.TotalOrderDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.TotalDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.DiscountPercentage = _commonProperties.OrderDiscount?.Percentage ?? 0;
        _commonProperties.OrderDto.DiscountReason = _commonProperties.OrderDiscount?.DiscountReason;
        _commonProperties.OrderDto.DiscountType = _commonProperties.OrderDiscount?.DiscountType;
        _commonProperties.OrderDto.DiscountByName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.DiscountTime = DateTime.Now;
        _commonProperties.OrderDto.GrandTotal = _commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0;
        _commonProperties.OrderDto.OrderDate = DateTime.Now;
        _commonProperties.OrderDto.OrderState = "Completed";
        _commonProperties.OrderDto.OrderNotice = _commonProperties.OrderNote;
        _commonProperties.OrderDto.OrderDetails = _commonProperties.TableItems?.ToList() ?? new List<TableItem>();

        _commonProperties.OrderDto.TakeAwayCustomerName = customerName;
        _commonProperties.OrderDto.TakeawayCustomerPhone = customerPhone;
        _commonProperties.OrderDto.CustomerName = customerName;
        _commonProperties.OrderDto.CustomerPhone = customerPhone;
        _commonProperties.OrderDto.OrderSettings = _commonProperties.OrderSettings;
        _commonProperties.OrderDto.MachineName = Environment.MachineName;
    }

    private async Task PrintTakeAwayLocally(OrderDto order, OrderDto createdOrder, bool isCopy = false, string? printerName = null)
    {
        var branches = await _branchService.GetBranches();
        var currentBranch = branches.FirstOrDefault(b => b.Id == order.BranchId);
        
        var receipt = new Receipt()
        {
            Id = createdOrder.OrderId,
            StoreName = order.BranchName ?? "Store",
            CashierName = order.CashierName ?? "Cashier",
            ReceiptType = order.OrderType ?? "TakeAway",
            DateCreated = createdOrder.OrderDate ?? DateTime.Now,
            PaymentMethod = order.PaymentMethod.ToString() ?? "Cash",
            FooterMessage = order.FooterMessage ?? "",
            LogoPath = LogoPath,
            LogoWidth = currentBranch?.LogoWidth ?? 150,
            TotalAmount = order.GrandTotal,
            Discount = order.TotalOrderDiscount,
            Tax = order.Tax,
            Services = order.Services,
            SubTotal = order.SubTotal,
            Items = order.OrderDetails ?? new List<TableItem>(),
            IsHospitality = order.IsHospitality ?? false,
            HospitalityResponsibleName = order.HospitalityResponsibleName,
            HospitalityReason = order.HospitalityReason,
            IsStaffMeal = order.IsStaffMeal ?? false,
            StaffMealEmployeeName = order.StaffMealEmployeeName
        };

        var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == "TakeAway");
        int currentPrintCount = 0;
        try
        {
            if (!isCopy)
                currentPrintCount = await _orderSettingsService.IncrementPrintCountAsync(createdOrder.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for TakeAway order {Id}", createdOrder.Id);
        }

        if (orderSettings != null && orderSettings.CustomerReceiptCount > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            var customerKitchen = kitchens?.ElementAtOrDefault(0);

            if (customerKitchen != null)
            {
                var printers = customerKitchen.KitchenPrinters?.FirstOrDefault();
                if (printers != null)
                {
                    string? originalPath = null;
                    string? copyPath = null;
                    var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                    Directory.CreateDirectory(reportsFolder);

                    for (int i = 1; i <= orderSettings.CustomerReceiptCount; i++)
                    {
                        var targetPrinter = printerName;
                        if (string.IsNullOrEmpty(targetPrinter))
                            targetPrinter = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                        
                        if (string.IsNullOrEmpty(targetPrinter)) continue;

                        bool isActuallyCopy = isCopy || (currentPrintCount > 1) || (i > 1);
                        string pathToPrint;
                        
                        if (isActuallyCopy)
                        {
                            if (copyPath == null)
                            {
                                receipt.IsCopy = true;
                                var document = new ReceiptDocument(receipt, order.OrderDetails ?? new List<TableItem>());
                                copyPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_TakeAway_Copy_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(copyPath);
                            }
                            pathToPrint = copyPath;
                        }
                        else
                        {
                            if (originalPath == null)
                            {
                                receipt.IsCopy = false;
                                var document = new ReceiptDocument(receipt, order.OrderDetails ?? new List<TableItem>());
                                originalPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_TakeAway_Original_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(originalPath);
                            }
                            pathToPrint = originalPath;
                        }

                        try { await _printerServices.PrintPdfAsync(pathToPrint, targetPrinter); }
                        catch (Exception ex) { Log.Error(ex, "Error printing TakeAway to {Printer}", targetPrinter); }
                    }
                }
            }
        }
    }

    private async Task PrintBackupReceiptsLocally(OrderDto order, OrderDto createdOrder, bool isReprint = false, int? reprintCount = null, bool isCopy = false, string? targetPrinterName = null)
    {
        if (order.OrderDetails == null) return;

        var itemsToProcess = isReprint 
            ? order.OrderDetails 
            : order.OrderDetails.Where(i => i.DatabaseId == 0).ToList();

        if (itemsToProcess.Count == 0)
        {
            Log.Information("PrintBackupReceiptsLocally: No items to process.");
            return;
        }

        var filteredItems = itemsToProcess
            .Where(i =>
                (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem != false) ||
                (i.PrintInBackupReceiptFromCategory == false && i.PrintInBackupReceiptFromItem == true) ||
                (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem == null) ||
                (i.PrintInBackupReceiptFromCategory == null && i.PrintInBackupReceiptFromItem == null) || 
                (i.PrintInBackupReceiptFromCategory == null && i.PrintInBackupReceiptFromItem == true)
            )
            .ToList();

        if (filteredItems.Count == 0)
        {
            Log.Information("PrintBackupReceiptsLocally: No filtered items for backup receipt.");
            return;
        }

        var receipt = new KitchenReceipt()
        {
            Id = createdOrder.OrderId,
            CashierName = order.CashierName!,
            OrderType = order.OrderType ?? "TakeAway",
            DateCreated = createdOrder.OrderDate.HasValue ? new DateTimeOffset(createdOrder.OrderDate.Value) : DateTimeOffset.Now,
            Items = filteredItems,
            KitchenNote = order.OrderNotice!,
            KitchenType = "Packup",
            TableName = order.TableName,
            IsCopy = isCopy,
            IsArabic = true
        };

        var document = new KitchenReceiptDocument(receipt, filteredItems);
        var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        Directory.CreateDirectory(reportsFolder);
        var outputPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_{order.OrderType ?? "Type"}_Backup_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        try
        {
            document.GeneratePdf(outputPath);

            var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == order.OrderType);
            var receiptCount = reprintCount ?? orderSettings?.FullKitchenReceiptCount ?? 0;

            if (receiptCount > 0)
            {
                var kitchens = await _printerServices.GetKitchenTypesAsync();
                // Use index 1 if available, otherwise index 0, otherwise first available
                var backupKitchen = kitchens?.ElementAtOrDefault(1) ?? null;
                
                if (backupKitchen != null)
                {
                    var printers = backupKitchen.KitchenPrinters?.FirstOrDefault();
                    if (printers != null)
                    {
                        for (int i = 1; i <= receiptCount; i++)
                        {
                            var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                            if (!string.IsNullOrEmpty(printerName))
                            {
                                Log.Information("Printing backup kitchen ticket to {Printer}", printerName);
                                await _printerServices.PrintPdfAsync(outputPath, printerName);
                            }
                        }
                    }
                    else
                    {
                        Log.Warning("Backup kitchen {KitchenName} has no printers configured.", backupKitchen.KitchenName);
                    }
                }
                else
                {
                    Log.Warning("No valid backup kitchen found to print full receipt.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print backup receipt locally");
        }
    }

    private async Task PrintKitchenReceiptsLocally(OrderDto order, OrderDto createdOrder, bool isReprint = false, int? reprintCount = null, bool isCopy = false, string? targetPrinterName = null)
    {
        var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == order.OrderType);
        var receiptCount = reprintCount ?? orderSettings?.SeparateReceiptCount ?? 0;

        if (receiptCount <= 0 || order.OrderDetails == null)
        {
            Log.Information("PrintKitchenReceiptsLocally: Skipping. receiptCount: {receiptCount}, hasDetails: {hasDetails}", receiptCount, order.OrderDetails != null);
            return;
        }

        var itemsToProcess = isReprint 
            ? order.OrderDetails 
            : order.OrderDetails.Where(i => i.DatabaseId == 0).ToList();

        if (itemsToProcess.Count == 0)
        {
            Log.Information("PrintKitchenReceiptsLocally: No items to process (isReprint={isReprint}).", isReprint);
            return;
        }

        var kitchens = await _printerServices.GetKitchenTypesAsync();
        if (kitchens == null || !kitchens.Any())
        {
            Log.Warning("PrintKitchenReceiptsLocally: No kitchens found for device {Device}", Environment.MachineName);
            return;
        }

        // Group by Kitchen ID
        var groups = itemsToProcess
            .Where(item => item.ItemKitchenTypeId.HasValue || item.CategoryKitchenTypeId.HasValue)
            .GroupBy(item => item.ItemKitchenTypeId ?? item.CategoryKitchenTypeId!.Value)
            .ToList();

        Log.Information("PrintKitchenReceiptsLocally: Found {count} kitchen groups to process.", groups.Count);

        foreach (var group in groups)
        {
            var kitchenId = group.Key;
            var kitchenItems = group.ToList();
            
            // Find kitchen by ID in memory
            var kitchen = kitchens.FirstOrDefault(k => k.Id == kitchenId);
            if (kitchen == null)
            {
                Log.Warning("PrintKitchenReceiptsLocally: Kitchen ID {Id} not configured for this device. Skipping {Count} items.", kitchenId, kitchenItems.Count);
                continue;
            }

            // Skip Customer and Backup kitchens — they are handled by Customer & Backup print methods and must not be duplicated here
            if (kitchen.KitchenName != null && 
                (kitchen.KitchenName.Equals("Backup", StringComparison.OrdinalIgnoreCase) || 
                 kitchen.KitchenName.Equals("Packup", StringComparison.OrdinalIgnoreCase) || 
                 kitchen.KitchenName.Equals("Customer", StringComparison.OrdinalIgnoreCase)))
            {
                Log.Information("PrintKitchenReceiptsLocally: Skipping default kitchen '{KitchenName}' to prevent duplicate backup print.", kitchen.KitchenName);
                continue;
            }

            var printers = kitchen.KitchenPrinters?.FirstOrDefault();
            if (printers == null && string.IsNullOrEmpty(targetPrinterName))
            {
                Log.Warning("PrintKitchenReceiptsLocally: Kitchen {KitchenName} has no printer mapping.", kitchen.KitchenName);
                continue;
            }

            var receipt = new KitchenReceipt()
            {
                Id = createdOrder.OrderId,
                CashierName = order.CashierName!,
                OrderType = order.OrderType ?? "TakeAway",
                DateCreated = createdOrder.OrderDate.HasValue ? new DateTimeOffset(createdOrder.OrderDate.Value) : DateTimeOffset.Now,
                Items = kitchenItems,
                KitchenNote = order.OrderNotice!,
                KitchenType = kitchen.KitchenName ?? $"Kitchen_{kitchen.Id}",
                TableName = order.TableName,
                IsCopy = isCopy,
                IsArabic = true
            };

            var document = new KitchenReceiptDocument(receipt, kitchenItems);
            var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(reportsFolder);
            var outputPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_{order.OrderType}_{receipt.KitchenType}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            try
            {
                document.GeneratePdf(outputPath);
                
                for (int i = 1; i <= receiptCount; i++)
                {
                    var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        Log.Information("Printing separate kitchen ticket for {Kitchen} to {Printer}", kitchen.KitchenName, printerName);
                        await _printerServices.PrintPdfAsync(outputPath, printerName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error printing separate kitchen ticket for {KitchenName}", kitchen.KitchenName);
            }
        }
    }

    public async Task<bool> PrintDeliveryOrder(decimal paid = 0)
    {
        await BackupDeliveryOrderDtoDetails(paid);

        _commonProperties.OrderDto!.SkipPrintingOnServer = true;
        
        OrderDto? result;
        bool isFollowUp = _commonProperties.UpdateDeliveryOrder;

        if (_commonProperties.UpdateDeliveryOrder)
            result = await _orderSettingsService.UpdateOrderAsync(_commonProperties.OrderDto!);
        else
            result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);

        if (result is null)
            return false;

        _commonProperties.CurrentOrderId = result.OrderId + 1;
        if (result.OrderDate.HasValue)
            _commonProperties.PosDate = DateOnly.FromDateTime(result.OrderDate.Value);

        try
        {
            decimal? parentDeliveryFees = null;
            if (isFollowUp && _commonProperties.OrderDto.ParentOrderId.HasValue)
               parentDeliveryFees = _commonProperties.OrderDto.DeliveryFees;

            await PrintDeliveryLocally(_commonProperties.OrderDto!, result, isFollowUp, parentDeliveryFees);

            var orderSettings = _commonProperties.OrderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");
            if (orderSettings != null)
            {
                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(_commonProperties.OrderDto!, result, isReprint: !_commonProperties.UpdateDeliveryOrder);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(_commonProperties.OrderDto!, result, isReprint: !_commonProperties.UpdateDeliveryOrder);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print delivery order locally");
        }

        return true;
    }

    private async Task BackupDeliveryOrderDtoDetails(decimal paid = 0)
    {
        if (!_commonProperties.UpdateDeliveryOrder)
            _commonProperties.OrderDto!.OrderId = _commonProperties.CurrentOrderId;
        
        _commonProperties.OrderDto!.OrderType = "Delivery";
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.BranchId = _commonProperties.BranchDetails!.Id;
        _commonProperties.OrderDto.BranchName = _commonProperties.BranchDetails.Name;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.FooterMessage = _commonProperties.DeliverySettings?.OrderStatment ?? "";
        _commonProperties.OrderDto.PaymentMethod = _commonProperties.SelectedPaymentMethod;
        _commonProperties.OrderDto.Paid = paid > 0.00m ? paid : (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0);
        _commonProperties.OrderDto.SubTotal = _commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 0 ? _commonProperties._financeSettingsList[0].Value : 0;
        _commonProperties.OrderDto.Services = _commonProperties.DeliverySettings?.Service ?? 0;
        _commonProperties.OrderDto.Tax = _commonProperties.DeliverySettings?.Tax ?? 0;
        _commonProperties.OrderDto.TotalOrderDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.TotalDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.DiscountPercentage = _commonProperties.OrderDiscount?.Percentage ?? 0;
        _commonProperties.OrderDto.DiscountReason = _commonProperties.OrderDiscount?.DiscountReason;
        _commonProperties.OrderDto.DiscountType = _commonProperties.OrderDiscount?.DiscountType;
        _commonProperties.OrderDto.DiscountByName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.DiscountTime = DateTime.Now;
        _commonProperties.OrderDto.GrandTotal = _commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0;
        _commonProperties.OrderDto.OrderDate = DateTime.Now;
        if (!_commonProperties.UpdateDeliveryOrder)
            _commonProperties.OrderDto.OrderState = "Pending";
        
        _commonProperties.OrderDto.OrderNotice = _commonProperties.OrderNote;
        _commonProperties.OrderDto.OrderDetails = _commonProperties.TableItems?.ToList() ?? new List<TableItem>();


        _commonProperties.OrderDto.CustomerName = _commonProperties.CustomerDetails?.CustomerName;
        _commonProperties.OrderDto.Phone1 = _commonProperties.CustomerDetails?.FirstPhoneNumber;
        _commonProperties.OrderDto.Phone2 = _commonProperties.CustomerDetails?.SecondPhoneNumber;
        _commonProperties.OrderDto.StreetName = _commonProperties.CustomerDetails?.ClientAddress;
        _commonProperties.OrderDto.ZoneBonus = _commonProperties.CustomerDetails?.ZoneBonus ?? 0;
        _commonProperties.OrderDto.DeliveryFees = _commonProperties.CustomerDetails?.ZoneFees; 
        _commonProperties.OrderDto.ZoneName = _commonProperties.CustomerDetails?.ZoneName;
        _commonProperties.OrderDto.AddressNotice = _commonProperties.CustomerDetails?.AddressNote;
        _commonProperties.OrderDto.HomeNum = _commonProperties.CustomerDetails?.HomeNumber;
        _commonProperties.OrderDto.FloorNum = _commonProperties.CustomerDetails?.FloorNumber;
        _commonProperties.OrderDto.ApartmentNum = _commonProperties.CustomerDetails?.FlatNumber;
        
        _commonProperties.OrderDto.TakerID = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.TakerName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.CustomerID = _commonProperties.CustomerDetails?.Id;
        _commonProperties.OrderDto.TitleName = _commonProperties.CustomerDetails?.ClientTitle;
        _commonProperties.OrderDto.ZoneID = _commonProperties.CustomerDetails?.ZoneID;

        _commonProperties.OrderDto.OrderSettings = _commonProperties.OrderSettings;
        _commonProperties.OrderDto.MachineName = Environment.MachineName;

        if (_commonProperties.CustomerDetails?.BranchId != null)
        {
            var branches = await _branchService.GetBranches();
            var branch = branches.FirstOrDefault(b => b.Id == _commonProperties.CustomerDetails.BranchId);
            _commonProperties.OrderDto.DeliveryBranchUrl = branch?.ApiUrl;
        }
    }

    private async Task PrintDeliveryLocally(OrderDto orderDto, OrderDto createdOrder, bool isFollowUp = false, decimal? parentDeliveryFees = null, bool isCopy = false, string? printerName = null)
    {
        var branch = await _branchService.GetBranches();
        var currentBranch = branch.FirstOrDefault(b => b.Id == orderDto.BranchId);

        var receipt = new DeliveryReceipt()
        {
            Id = createdOrder.ParentOrderId ?? createdOrder.OrderId,
            ParentOrderId = createdOrder.ParentOrderId,
            IsFollowUp = isFollowUp,

            StoreName = orderDto.BranchName ?? "Store",
            CashierName = orderDto.CashierName ?? "Cashier",
            ReceiptType = orderDto.OrderType ?? "Delivery",
            DateCreated = createdOrder.OrderDate ?? DateTime.Now,
            PaymentMethod = orderDto.PaymentMethod.ToString() ?? "Cash",
            FooterMessage = orderDto.FooterMessage ?? "",
            LogoPath = LogoPath,
            LogoWidth = currentBranch?.LogoWidth ?? 150,
            CustomerName = orderDto.CustomerName,
            CustomerFirstPhone = orderDto.Phone1,
            CustomerSecondPhone = orderDto.Phone2,
            CustomerAddress = orderDto.StreetName,
            DeliveryName = orderDto.DriverName,
            
            DeliveryFees = (orderDto.DeliveryFees ?? 0) != 0 ? (orderDto.DeliveryFees ?? 0)
                         : (createdOrder.DeliveryFees ?? 0) != 0 ? (createdOrder.DeliveryFees ?? 0)
                         : (parentDeliveryFees ?? 0),

            ZoneName = orderDto.ZoneName,
            AddressNote = orderDto.AddressNotice,
            HomeNumber = orderDto.HomeNum,
            FloorNumber = orderDto.FloorNum,
            FlatNumber = orderDto.ApartmentNum,
            TotalAmount = orderDto.SubTotal,
            TotalOrder = orderDto.GrandTotal,
            IsHospitality = orderDto.IsHospitality ?? false,
            HospitalityResponsibleName = orderDto.HospitalityResponsibleName,
            HospitalityReason = orderDto.HospitalityReason,
            IsStaffMeal = orderDto.IsStaffMeal ?? false,
            StaffMealEmployeeName = orderDto.StaffMealEmployeeName
        };

        var orderSettings = orderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");
        int currentPrintCount = 0;
        try
        {
            if (!isCopy)
                currentPrintCount = await _orderSettingsService.IncrementPrintCountAsync(createdOrder.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for Delivery order {Id}", createdOrder.Id);
        }

        if (orderSettings != null && orderSettings.CustomerReceiptCount > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            var customerKitchen = kitchens?.ElementAtOrDefault(0);

            if (customerKitchen != null)
            {
                var printers = customerKitchen.KitchenPrinters?.FirstOrDefault();
                if (printers != null)
                {
                    string? originalPath = null;
                    string? copyPath = null;
                    var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                    Directory.CreateDirectory(reportsFolder);

                    for (int i = 1; i <= orderSettings.CustomerReceiptCount; i++)
                    {
                        var targetPrinter = printerName;
                        if (string.IsNullOrEmpty(targetPrinter))
                            targetPrinter = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;

                        if (string.IsNullOrEmpty(targetPrinter)) continue;

                        bool isActuallyCopy = isCopy || (currentPrintCount > 1) || (i > 1);
                        string pathToPrint;
                        
                        if (isActuallyCopy)
                        {
                            if (copyPath == null)
                            {
                                receipt.IsCopy = true;
                                var document = new DeliveryReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                copyPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_Delivery_Copy_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(copyPath);
                            }
                            pathToPrint = copyPath;
                        }
                        else
                        {
                            if (originalPath == null)
                            {
                                receipt.IsCopy = false;
                                var document = new DeliveryReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                originalPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_Delivery_Original_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(originalPath);
                            }
                            pathToPrint = originalPath;
                        }

                        try { await _printerServices.PrintPdfAsync(pathToPrint, targetPrinter); }
                        catch (Exception ex) { Log.Error(ex, "Error printing Delivery to {Printer}", targetPrinter); }
                    }
                }
            }
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _recentlyPrintedOrders = new();

    public async Task PrintReceivedOrderAsync(OrderDto order)
    {
        try
        {
            var orderKey = $"{order.Id}_{order.OrderId}";
            if (_recentlyPrintedOrders.TryGetValue(orderKey, out var lastPrintTime) && (DateTime.UtcNow - lastPrintTime).TotalSeconds < 15)
            {
                Log.Warning("[Print Guard] Order {Key} was already printed {Seconds:F1}s ago. Skipping duplicate print.", orderKey, (DateTime.UtcNow - lastPrintTime).TotalSeconds);
                return;
            }
            _recentlyPrintedOrders[orderKey] = DateTime.UtcNow;

            // Clean up keys older than 1 minute to keep dictionary compact
            foreach (var kvp in _recentlyPrintedOrders)
            {
                if ((DateTime.UtcNow - kvp.Value).TotalMinutes > 1)
                    _recentlyPrintedOrders.TryRemove(kvp.Key, out _);
            }

            // Always fetch fresh settings for this machine to respect any BackOffice updates at branch
            try
            {
                var freshSettings = await _orderSettingsService.GetOrderSettingsAsync(Environment.MachineName);
                if (freshSettings != null && freshSettings.Any())
                {
                    _commonProperties.OrderSettings = freshSettings;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[PrintReceivedOrderAsync] Failed to fetch fresh order settings, falling back to cached settings.");
            }

            order.OrderSettings = _commonProperties.OrderSettings;

            await PrintDeliveryLocally(order, order);

            var orderSettings = _commonProperties.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");

            if (orderSettings != null)
            {
                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(order, order, isReprint: true);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(order, order, isReprint: true);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print received order locally");
        }
    }

    public async Task PrintDispatchOrderAsync(OrderDto order)
    {
        try
        {
            order.OrderSettings = _commonProperties.OrderSettings;

            await PrintDispatchLocally(order, order);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to Print Dispatch Order Locally");
        }
    }

    private async Task PrintDispatchLocally(OrderDto orderDto, 
        OrderDto createdOrder,
        bool isFollowUp = false, 
        decimal? parentDeliveryFees = null)
    {
        var branch = await _branchService.GetBranches();
        var currentBranch = branch.FirstOrDefault(b => b.Id == orderDto.BranchId);

        var receipt = new DeliveryReceipt()
        {
            Id = createdOrder.ParentOrderId ?? createdOrder.OrderId,
            ParentOrderId = createdOrder.ParentOrderId,
            IsFollowUp = isFollowUp,

            StoreName = orderDto.BranchName ?? "Store",
            CashierName = orderDto.CashierName ?? "Cashier",
            ReceiptType = orderDto.OrderType ?? "Delivery",
            DateCreated = createdOrder.OrderDate ?? DateTime.Now,
            PaymentMethod = orderDto.PaymentMethod.ToString() ?? "Cash",
            FooterMessage = orderDto.FooterMessage ?? "",
            LogoPath = LogoPath,
            LogoWidth = currentBranch?.LogoWidth ?? 150,
            CustomerName = orderDto.CustomerName,
            CustomerFirstPhone = orderDto.Phone1,
            CustomerSecondPhone = orderDto.Phone2,
            CustomerAddress = orderDto.StreetName,
            DeliveryName = orderDto.DriverName,
            
            DeliveryFees = (orderDto.DeliveryFees ?? 0) != 0 ? (orderDto.DeliveryFees ?? 0)
                         : (createdOrder.DeliveryFees ?? 0) != 0 ? (createdOrder.DeliveryFees ?? 0)
                         : (parentDeliveryFees ?? 0),

            ZoneName = orderDto.ZoneName,
            AddressNote = orderDto.AddressNotice,
            HomeNumber = orderDto.HomeNum,
            FloorNumber = orderDto.FloorNum,
            FlatNumber = orderDto.ApartmentNum,
            TotalAmount = orderDto.SubTotal,
            TotalOrder = orderDto.GrandTotal
        };

        var orderSettings = orderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");
        int currentPrintCount = 0;
        try
        {
            currentPrintCount = await _orderSettingsService.IncrementPrintCountAsync(createdOrder.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for Dispatch order {Id}", createdOrder.Id);
        }

        if (orderSettings != null && orderSettings.CustomerReceiptCount > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            var customerKitchen = kitchens?.ElementAtOrDefault(0);

            if (customerKitchen != null)
            {
                var printers = customerKitchen.KitchenPrinters?.FirstOrDefault();
                if (printers != null)
                {

                    string? originalPath = null;
                    string? copyPath = null;
                    var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                    Directory.CreateDirectory(reportsFolder);

                    for (int i = 1; i <= orderSettings.CustomerReceiptCount; i++)
                    {
                        var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                        if (string.IsNullOrEmpty(printerName)) continue;

                        bool isActuallyCopy = (currentPrintCount > 1) || (i > 1);

                        string pathToPrint;
                        if (isActuallyCopy)
                        {
                            if (copyPath == null)
                            {
                                receipt.IsCopy = true;
                                var document = new DeliveryReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                copyPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_Dispatch_Copy_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(copyPath);
                            }
                            pathToPrint = copyPath;
                        }
                        else
                        {
                            if (originalPath == null)
                            {
                                receipt.IsCopy = false;
                                var document = new DeliveryReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                originalPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_Dispatch_Original_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(originalPath);
                            }
                            pathToPrint = originalPath;
                        }

                        try
                        {
                            await _printerServices.PrintPdfAsync(pathToPrint, printerName);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error printing Dispatch to printer {PrinterName}", printerName);
                        }
                    }
                }
            }
        }
    }
    public async Task PrintVoidReceiptAsync(OrderDto order, List<TableItem> voidedItems)
    {
        if (voidedItems == null || !voidedItems.Any()) return;
        
        // Use KitchenReceipt but set Type to VOID
        await PrintVoidReceiptInternal(order.OrderId, order.CashierName, order.OrderType ?? "Order", order.OrderDate, order.OrderNotice, order.TableName, voidedItems);
    }
    
    public async Task PrintDineInVoidReceiptAsync(DineInOrderDto order, List<TableItem> voidedItems)
    {
        if (voidedItems == null || !voidedItems.Any()) return;
        
        await PrintVoidReceiptInternal(order.Id, order.CashierName, "DineIn", order.OrderDateTime, order.OrderNotice, order.TableName, voidedItems);
    }

    private async Task PrintVoidReceiptInternal(int orderId, string? cashier, string orderType, DateTime? date, string? notice, string? tableName, List<TableItem> items)
    {
        // Similar to PrintKitchenReceiptsLocally but forced to VOID type
        var kitchens = await _printerServices.GetKitchenTypesAsync();
        if (kitchens == null || !kitchens.Any()) return;

        var groups = items
            .Where(item => item.ItemKitchenTypeId.HasValue || item.CategoryKitchenTypeId.HasValue)
            .GroupBy(item => item.ItemKitchenTypeId ?? item.CategoryKitchenTypeId!.Value)
            .ToList();

        foreach (var group in groups)
        {
            var kitchenId = group.Key;
            var kitchenItems = group.ToList();
            var kitchen = kitchens.FirstOrDefault(k => k.Id == kitchenId);
            
            if (kitchen == null) continue;
            var printers = kitchen.KitchenPrinters?.FirstOrDefault();
            if (printers == null) continue;

            var receipt = new KitchenReceipt()
            {
                Id = orderId,
                CashierName = cashier ?? "Cashier",
                OrderType = orderType, // Will be displayed
                DateCreated = date.HasValue ? new DateTimeOffset(date.Value) : DateTimeOffset.Now,
                Items = kitchenItems,
                KitchenNote = notice ?? "",
                KitchenType = $"{kitchen.KitchenName ?? "Kitchen"} - VOID", // Appending VOID
                TableName = tableName
            };
            
            // Add a special flag or handle in PDF generation for RED VOID text if possible, 
            // but for now relying on the KitchenType name containing "VOID".
            
            var document = new KitchenReceiptDocument(receipt, kitchenItems);
            var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(reportsFolder);
            var outputPath = Path.Combine(reportsFolder, $"Order_{orderId}_VOID_{receipt.KitchenType}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            try
            {
                document.GeneratePdf(outputPath);
                
                // Print 1 copy to the main printer
                var printerName = printers.Copy1; 
                if (!string.IsNullOrEmpty(printerName))
                {
                    Log.Information("Printing VOID ticket for {Kitchen} to {Printer}", kitchen.KitchenName, printerName);
                    await _printerServices.PrintPdfAsync(outputPath, printerName);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error printing VOID ticket for {KitchenName}", kitchen.KitchenName);
            }
        }

        // ─── Also print VOID on the Backup/Packup printer ───
        try
        {
            var backupItems = items
                .Where(i => i.PrintInBackupReceiptFromItem ?? i.PrintInBackupReceiptFromCategory ?? true)
                .ToList();

            if (backupItems.Any())
            {
                var backupReceipt = new KitchenReceipt()
                {
                    Id = orderId,
                    CashierName = cashier ?? "Cashier",
                    OrderType = orderType,
                    DateCreated = date.HasValue ? new DateTimeOffset(date.Value) : DateTimeOffset.Now,
                    Items = backupItems,
                    KitchenNote = notice ?? "",
                    KitchenType = "Packup",
                    TableName = tableName,
                    IsVoid = true
                };

                var backupDocument = new KitchenReceiptDocument(backupReceipt, backupItems);
                var reportsFolder2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                Directory.CreateDirectory(reportsFolder2);
                var backupOutputPath = Path.Combine(reportsFolder2, $"Order_{orderId}_VOID_Backup_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                backupDocument.GeneratePdf(backupOutputPath);

                var backupKitchen = kitchens.FirstOrDefault(k => k.KitchenName != null &&
                    (k.KitchenName.Equals("Backup", StringComparison.OrdinalIgnoreCase) ||
                     k.KitchenName.Equals("Packup", StringComparison.OrdinalIgnoreCase)))
                    ?? kitchens.ElementAtOrDefault(1)
                    ?? kitchens.FirstOrDefault();

                if (backupKitchen != null)
                {
                    var backupPrinters = backupKitchen.KitchenPrinters?.FirstOrDefault(p => p.DeviceName == Environment.MachineName)
                        ?? backupKitchen.KitchenPrinters?.FirstOrDefault();

                    if (backupPrinters != null)
                    {
                        var settingsOrderType = (orderType == "Staff" || orderType == "Hospitality") ? "TakeAway" : orderType;
                        var orderSettings = _commonProperties.OrderSettings?.FirstOrDefault(o => o.OrderType == settingsOrderType);
                        var receiptCount = orderSettings?.FullKitchenReceiptCount ?? 1;

                        for (int idx = 1; idx <= receiptCount; idx++)
                        {
                            var backupPrinterName = typeof(KitchenPrinters).GetProperty($"Copy{idx}")?.GetValue(backupPrinters) as string;
                            if (!string.IsNullOrEmpty(backupPrinterName))
                            {
                                Log.Information("Printing VOID backup receipt to {Printer}", backupPrinterName);
                                await _printerServices.PrintPdfAsync(backupOutputPath, backupPrinterName);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error printing VOID backup receipt for order {OrderId}", orderId);
        }
    }

    public async Task PrintSalesSummaryAsync(SalesSummaryDto summary, List<SalesItemSummaryDto> items, string? printerName = null, bool useA4 = false, bool isArabic = true)
    {
        try
        {
            var branch = await _branchService.GetBranches();
            var currentBranch = branch.FirstOrDefault(b => b.Id == _commonProperties.BranchDetails?.Id);
            
            var isThermal = !useA4;
            var lang = isArabic ? "ar" : "en";
            var document = new BackOfficeSalesSummaryDocument(
                summary, items, 
                currentBranch?.Name ?? "Store", 
                summary.PosDate, summary.PosDate, 
                isThermal, 
                lang);
            
            var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(reportsFolder);
            var formatSuffix = useA4 ? "A4" : "Cashier";
            var langSuffix = isArabic ? "AR" : "EN";
            var outputPath = Path.Combine(reportsFolder, $"SalesSummary_{summary.PosDate:yyyyMMdd}_{formatSuffix}_{langSuffix}_{DateTime.Now:HHmmss}.pdf");
            
            document.GeneratePdf(outputPath);
            
            if (string.IsNullOrEmpty(printerName))
            {
                printerName = new System.Drawing.Printing.PrinterSettings().PrinterName;
            }
            
            if (string.IsNullOrEmpty(printerName))
            {
                var kitchens = await _printerServices.GetKitchenTypesAsync();
                var mainKitchen = kitchens?.ElementAtOrDefault(0); // TakeAway/Main printer usually
                printerName = mainKitchen?.KitchenPrinters?.FirstOrDefault()?.Copy1;
            }

            if (!string.IsNullOrEmpty(printerName))
            {
                Log.Information("Printing Sales Summary ({Format}) to: {Printer}", formatSuffix, printerName);
                await _printerServices.PrintPdfAsync(outputPath, printerName);
            }
            else
            {
                Log.Warning("No printer found (Default or Cashier) to print Sales Summary.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print sales summary");
        }
    }

    public async Task PrintEndDayReportAsync(SalesSummaryDto summary, List<SalesItemSummaryDto> items, string? printerName = null, bool useA4 = false, bool isArabic = true)
    {
        try
        {
            var branches = await _branchService.GetBranches();
            var currentBranch = branches.FirstOrDefault();

            
            var format = useA4 ? ReportPageFormat.A4 : ReportPageFormat.Cashier;
            var document = new EndDayDocument(summary, items, currentBranch?.Name ?? "Store", LogoPath, format, isArabic);
            
            var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(reportsFolder);
            var formatSuffix = useA4 ? "A4" : "Cashier";
            var langSuffix = isArabic ? "AR" : "EN";
            var outputPath = Path.Combine(reportsFolder, $"EndDay_{summary.PosDate:yyyyMMdd}_{formatSuffix}_{langSuffix}_{DateTime.Now:HHmmss}.pdf");
            
            document.GeneratePdf(outputPath);
            
            if (string.IsNullOrEmpty(printerName))
            {
                printerName = new System.Drawing.Printing.PrinterSettings().PrinterName;
            }
            
            if (string.IsNullOrEmpty(printerName))
            {
                var kitchens = await _printerServices.GetKitchenTypesAsync();
                var mainKitchen = kitchens?.ElementAtOrDefault(0);
                printerName = mainKitchen?.KitchenPrinters?.FirstOrDefault()?.Copy1;
            }

            if (!string.IsNullOrEmpty(printerName))
            {
                Log.Information("Printing End Day Report ({Format}) to: {Printer}", formatSuffix, printerName);
                await _printerServices.PrintPdfAsync(outputPath, printerName);
            }
            else
            {
                Log.Warning("No printer found to print End Day Report.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print end day report");
        }
    }

    public async Task PrintStaffPerformanceAsync(SalesSummaryDto summary, string? printerName = null, bool useA4 = false, bool isArabic = true, bool showOrders = false, string? specificStaffId = null)
    {
        try
        {
            var branches = await _branchService.GetBranches();
            var currentBranch = branches.FirstOrDefault();

            var document = new StaffPerformanceDocument(summary, currentBranch?.Name ?? "Store", !useA4, isArabic, showOrders, specificStaffId);
            
            var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(reportsFolder);
            var formatSuffix = useA4 ? "A4" : "Cashier";
            var langSuffix = isArabic ? "AR" : "EN";
            var outputPath = Path.Combine(reportsFolder, $"StaffPerformance_{summary.PosDate:yyyyMMdd}_{formatSuffix}_{langSuffix}_{DateTime.Now:HHmmss}.pdf");
            
            document.GeneratePdf(outputPath);
            
            if (string.IsNullOrEmpty(printerName))
            {
                printerName = new System.Drawing.Printing.PrinterSettings().PrinterName;
            }
            
            if (!string.IsNullOrEmpty(printerName))
            {
                Log.Information("Printing Staff Performance ({Format}) to: {Printer}", formatSuffix, printerName);
                await _printerServices.PrintPdfAsync(outputPath, printerName);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print staff performance report");
        }
    }

    public Task PrintDriverSettlementAsync(DriverSettlementDto settlement, DateTime posDate, string? printerName = null)
    {
        // TODO: Implement driver settlement printing when needed
        Log.Warning("PrintDriverSettlementAsync called but not yet implemented for BackOffice desktop.");
        return Task.CompletedTask;
    }

    public Task PrintAllDriversSettlementAsync(List<DriverSettlementDto> settlements, DateTime posDate, string? printerName = null)
    {
        Log.Warning("PrintAllDriversSettlementAsync called but not yet implemented for BackOffice desktop.");
        return Task.CompletedTask;
    }


    public Task<bool> PrintStaffMealOrder(string employeeName, List<TableItem> items)
    {
        Log.Warning("PrintStaffMealOrder is not implemented for BackOffice desktop.");
        return Task.FromResult(false);
    }

    public Task<bool> PrintHospitalityOrder(string reason, List<TableItem> items)
    {
        Log.Warning("PrintHospitalityOrder is not implemented for BackOffice desktop.");
        return Task.FromResult(false);
    }

    public Task PrintFollowUpOrderAsync(OrderDto order, List<TableItem>? addedItems = null)
    {
        throw new NotImplementedException();
    }
}
