namespace POS.Services.KitchenServices;

public class KitchenPrinterService : IKitchenPrintersService
{ 
    private readonly IUnitOfWork _unitOfWork;

    public KitchenPrinterService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<KitchenPrinters?> CreatePrinterAsync(KitchenPrinters printer)
    {
        try
        {
            await _unitOfWork.Repository<KitchenPrinters>().AddAsync(printer);
            var result = await _unitOfWork.CompleteAsync();
            return result > 0 ? printer : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating KitchenPrinter");
            return null;
        }
    }

    public async Task<bool> DeletePrinter(KitchenPrinters printer)
    {
        try
        {
            _unitOfWork.Repository<KitchenPrinters>().Delete(printer);
            return await _unitOfWork.CompleteAsync() > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting KitchenPrinter with Id {Id}", printer.Id);
            return false;
        }
    }

    public async Task<IReadOnlyList<KitchenPrinters>?> GetAllPrintersAsync()
    {
        try
        {
            return await _unitOfWork.Repository<KitchenPrinters>().GetAllAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving KitchenPrinters");
            return null;
        }
    }

    public async Task<KitchenPrinters?> GetPrinterByIdAsync(int id)
    {
        try
        {
            return await _unitOfWork.Repository<KitchenPrinters>().GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving KitchenPrinter with Id {id}", id);
            return null;
        }
    }

    public async Task<KitchenPrinters?> UpdatePrinterAsync(KitchenPrinters oldPrinter, KitchenPrinters newPrinter)
    {
        try
        {
            oldPrinter.DeviceName = newPrinter.DeviceName;
            oldPrinter.Copy1 = newPrinter.Copy1;
            oldPrinter.Copy2 = newPrinter.Copy2;
            oldPrinter.Copy3 = newPrinter.Copy3;
            oldPrinter.Copy4 = newPrinter.Copy4;
            oldPrinter.Copy5 = newPrinter.Copy5;
            oldPrinter.Copy6 = newPrinter.Copy6;
            oldPrinter.Copy7 = newPrinter.Copy7;
            oldPrinter.Copy8 = newPrinter.Copy8;
            oldPrinter.Copy9 = newPrinter.Copy9;
            oldPrinter.Copy10 = newPrinter.Copy10;

            _unitOfWork.Repository<KitchenPrinters>().Update(oldPrinter);
            var result = await _unitOfWork.CompleteAsync();

            return result > 0 ? oldPrinter : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating KitchenPrinter with Id {id}", oldPrinter.Id);
            return null;
        }
    }
}
