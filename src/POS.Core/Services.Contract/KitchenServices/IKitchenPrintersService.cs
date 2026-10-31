namespace POS.Core.Services.Contract.KitchenServices;

public interface IKitchenPrintersService
{
    public Task<KitchenPrinters?> CreatePrinterAsync(KitchenPrinters printer);
    public Task<IReadOnlyList<KitchenPrinters>?> GetAllPrintersAsync();
    public Task<KitchenPrinters?> GetPrinterByIdAsync(int id);
    public Task<KitchenPrinters?> UpdatePrinterAsync(KitchenPrinters oldPrinter, KitchenPrinters newPrinter);
    public Task<bool> DeletePrinter(KitchenPrinters printer);
}