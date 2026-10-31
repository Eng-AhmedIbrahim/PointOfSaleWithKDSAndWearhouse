namespace POS.Desktop.Services;

public class DesktopPrinterService : IPrinterServices
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly IConfiguration _configuration;

    public DesktopPrinterService(
        IHttpClientFactory httpClientFactory,
        ApiSettings apiSettings,
        IConfiguration configuration)
    {
        _apiSettings = apiSettings;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient(apiSettings.ApiName!);
    }

    #region Data Methods (Calling API)

    public async Task<KitchenType?> CreatePrinterAsync(KitchenType printer)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Printer/printer", printer);
        return await response.Content.ReadFromJsonAsync<KitchenType>();
    }

    public async Task<List<KitchenType>?> GetKitchenTypesAsync(string? deviceName = null)
    {
        var terminalName = deviceName ?? Environment.MachineName;
        return await _httpClient.GetFromJsonAsync<List<KitchenType>>($"api/Printer/kitchens?deviceName={terminalName}");
    }

    public async Task<KitchenType?> GetKitchenByIdAsync(int kitchenId)
    {
        return await _httpClient.GetFromJsonAsync<KitchenType>($"api/Printer/kitchen/{kitchenId}");
    }

    public async Task<KitchenType?> UpdateKitchenTypesAsync(KitchenType printer)
    {
        var response = await _httpClient.PutAsJsonAsync("api/Printer/printer", printer);
        return await response.Content.ReadFromJsonAsync<KitchenType>();
    }

    public async Task<OrderSetting?> CreateOrderSettingAsync(OrderSetting orderSetting)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Printer/order-setting", orderSetting);
        return await response.Content.ReadFromJsonAsync<OrderSetting>();
    }

    public async Task<OrderSetting?> UpdateOrderSettingAsync(OrderSetting orderSetting)
    {
        var response = await _httpClient.PutAsJsonAsync("api/Printer/order-setting", orderSetting);
        return await response.Content.ReadFromJsonAsync<OrderSetting>();
    }

    public async Task<List<OrderSetting>?> GetOrderSettingsAsync(string? deviceName = null)
    {
        var terminalName = deviceName ?? Environment.MachineName;
        return await _httpClient.GetFromJsonAsync<List<OrderSetting>>($"api/Printer/order-settings?deviceName={terminalName}");
    }

    public async Task<OrderSetting?> GetOrderSettingByIdAsync(int orderSettingId)
    {
        return await _httpClient.GetFromJsonAsync<OrderSetting>($"api/Printer/order-setting/{orderSettingId}");
    }

    #endregion

    #region Printing Methods (Local Library)

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task<List<string>> GetInstalledPrinters()
    {
        return await Task.Run(() =>
        {
            var printers = new List<string>();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }
            return printers;
        });
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task<bool> PrintPdfAsync(string pdfFilePath, string printerName)
    {
        var stopwatch = Stopwatch.StartNew(); // بدء قياس الوقت

        try
        {
            if (!File.Exists(pdfFilePath))
                throw new FileNotFoundException("PDF file not found.", pdfFilePath);

            // Manual Rendering (Using internal library as requested)
            byte[] pdfBytes = await File.ReadAllBytesAsync(pdfFilePath);
            using (var memoryStream = new MemoryStream(pdfBytes))
            using (var pdfDocument = PdfDocument.Load(memoryStream))
            {
                var printDoc = new PrintDocument
                {
                    PrinterSettings = new PrinterSettings { PrinterName = printerName },
                    // Use StandardPrintController to prevent the "Printing..." dialog box from appearing
                    PrintController = new StandardPrintController()
                };

                // Calculate width and height dynamically from PDF
                double pageWidthPoints = pdfDocument.PageSizes.Count > 0 ? pdfDocument.PageSizes[0].Width : 226; // Default to ~80mm if unknown
                double pageHeightPoints = pdfDocument.PageSizes.Count > 0 ? pdfDocument.PageSizes[0].Height : 842;
                
                int widthInHundredths = (int)(pageWidthPoints / 72.0 * 100.0);
                int heightInHundredths = (int)(pageHeightPoints / 72.0 * 100.0);
                
                // Determine paper name and kind
                string paperName = "CustomSize";
                int paperKindInt = (int)PaperKind.Custom;

                // Optimization: If it's a receipt (Width around 80mm / 315 hundredths)
                // we keep the 285 logic to avoid cutting from right on thermal printers.
                // Otherwise (e.g. A4 which is ~827 hundredths), we use the detected width.
                if (widthInHundredths < 450) 
                {
                    widthInHundredths = 285; 
                    paperName = "Receipt72mm";
                }
                else if (widthInHundredths >= 800 && widthInHundredths <= 850)
                {
                    paperName = "A4";
                    paperKindInt = (int)PaperKind.A4;
                }

                var customSize = new PaperSize(paperName, widthInHundredths, heightInHundredths)
                {
                    RawKind = paperKindInt
                };

                printDoc.DefaultPageSettings.PaperSize = customSize;
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                // FORCE High Resolution (Critical for correct scaling)
                // If we don't do this, some drivers default to screen resolution (96 DPI), causing small prints
                printDoc.DefaultPageSettings.PrinterResolution = printDoc.PrinterSettings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High) 
                    ?? new PrinterResolution { Kind = PrinterResolutionKind.Custom, X = 203, Y = 203 };

                int currentPage = 0;
                printDoc.PrintPage += (sender, e) =>
                {
                    if (currentPage < pdfDocument.PageCount)
                    {
                        // Optimization for Xprinter
                        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                        // CRITICAL FIX: Ensure scaling matches printer execution
                        // Never accept less than 203 DPI (Standard Thermal) for rendering
                        // This prevents 96 DPI (Screen) rendering which makes receipts look tiny
                        int dpiX = (int)e.Graphics.DpiX;
                        int dpiY = (int)e.Graphics.DpiY;

                        if (dpiX < 200) dpiX = 203;
                        if (dpiY < 200) dpiY = 203;

                        // Use the explicit resolution values if available in settings
                        if (printDoc.DefaultPageSettings.PrinterResolution.Kind != PrinterResolutionKind.Custom)
                        {
                            if (printDoc.DefaultPageSettings.PrinterResolution.X > 0) dpiX = printDoc.DefaultPageSettings.PrinterResolution.X;
                            if (printDoc.DefaultPageSettings.PrinterResolution.Y > 0) dpiY = printDoc.DefaultPageSettings.PrinterResolution.Y;
                        }

                        // Calculate dimensions in PIXELS for correct 1:1 scaling
                        // e.PageBounds provides 1/100 inch, but Render needs explicit PIXELS to match the scale
                        // This fixes the issue where passing Bounds (315 units) was interpreted as 315 pixels (tiny)
                        int widthPx = (int)((printDoc.DefaultPageSettings.PaperSize.Width / 100.0) * dpiX);
                        int heightPx = (int)((printDoc.DefaultPageSettings.PaperSize.Height / 100.0) * dpiY);

                        // Render centered or strictly at 0,0 within the printable area
                        pdfDocument.Render(currentPage, e.Graphics, dpiX, dpiY, new System.Drawing.Rectangle(0, 0, widthPx, heightPx), PdfRenderFlags.CorrectFromDpi);
                        
                        currentPage++;
                        e.HasMorePages = currentPage < pdfDocument.PageCount;
                    }
                };


                await Task.Run(() => printDoc.Print()); 
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Print failed: {ex.Message}");
            return false;
        }
        finally
        {
            stopwatch.Stop(); // إيقاف القياس
            Serilog.Log.Information($"Total time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        }

        return true;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task PrintPdfToMultipleAsync(string pdfFilePath, List<string> printerNames)
    {
        var printTasks = new List<Task>();

        foreach (var printerName in printerNames)
        {
            printTasks.Add(PrintPdfAsync(pdfFilePath, printerName));
        }

        try
        {
            await Task.WhenAll(printTasks);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Error occurred while printing to multiple: {ex.Message}");
            throw;
        }
    }

    public Task PrintPdfFromUrlAsync(string pdfUrl)
    {
        throw new NotImplementedException("PrintPdfFromUrlAsync is not implemented for Desktop yet.");
    }

    #endregion
}
