namespace POS.Core.Services.Contract.PrinterServices;

public interface IPrinterServices 
{
    public Task<KitchenType?> CreatePrinterAsync(KitchenType printer);
    public Task<List<KitchenType>?> GetKitchenTypesAsync(string? deviceName = null);
    public Task<KitchenType?> GetKitchenByIdAsync(int kitchenId);
    public Task<KitchenType?> UpdateKitchenTypesAsync(KitchenType printer);

    public Task<OrderSetting?> CreateOrderSettingAsync(OrderSetting orderSetting);
    public Task<OrderSetting?> UpdateOrderSettingAsync(OrderSetting orderSetting);
    public Task<List<OrderSetting>?> GetOrderSettingsAsync(string? deviceName = null);
    public Task<OrderSetting?> GetOrderSettingByIdAsync(int orderSettingId);

    public Task PrintPdfFromUrlAsync(string pdfUrl);
    public Task<List<string>> GetInstalledPrinters();

    public Task<bool> PrintPdfAsync(string pdfFilePath, string printerName);

    public Task PrintPdfToMultipleAsync(string pdfFilePath, List<string> printerNames);
}
