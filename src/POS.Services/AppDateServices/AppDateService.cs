namespace POS.Services.AppDateServices;

public class AppDateService : IAppDateService
{
    private readonly IUnitOfWork _unitOfWork;


    public AppDateService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<AppDate> GetAppDateAsync()
    {
         var appDates= await _unitOfWork.Repository<AppDate>().GetAllAsync();

        return appDates!.FirstOrDefault()??new();
    }

    public async Task<AppDate> UpdateAppDate()
    {
        try
        {
            var appDate = (await GetAppDateAsync());
            appDate.PosDate = appDate.PosDate.AddDays(1);
            appDate.StoreDate = appDate.StoreDate.AddDays(1);

            _unitOfWork.Repository<AppDate>().Update(appDate);
            await _unitOfWork.CompleteAsync();
            return appDate;
        }
        catch (Exception ex) 
        {
            Log.Error(ex.Message);
            return new();
        }
    }


    public async Task<AppDate> UpdateOrderNumber()
    {
        var appDate = await GetAppDateAsync();
        appDate.CurrentOrderNumber = appDate.CurrentOrderNumber + 1;

        _unitOfWork.Repository<AppDate>().Update(appDate);
        await _unitOfWork.CompleteAsync();
        return appDate;
    }

    public async Task<bool> CheckEndOfDayStatusAsync()
    {
        try
        {
            var appDate = await GetAppDateAsync();
            if (appDate == null) return false;

            // Check if any orders exist for today that are NOT in a final state
            // Final states are Completed, Voided, and Canceled.
            Expression<Func<Orders, bool>> criteria = o => 
                o.OrderDate.HasValue && 
                o.OrderDate.Value.Date == appDate.PosDate.Date &&
                o.OrderState != OrderStates.Completed &&
                o.OrderState != OrderStates.Voided &&
                o.OrderState != OrderStates.Canceled;

            var hasPending = await _unitOfWork.Repository<Orders>().ExistsAsync(criteria);
            return !hasPending;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to Check End Of Day Status");
            return false;
        }
    }

    public async Task<bool> CloseDayAsync()
    {
        try
        {
            var appDate = await GetAppDateAsync();
            if (appDate == null) return false;

            // Increment dates
            appDate.PosDate = appDate.PosDate.AddDays(1);
            appDate.StoreDate = appDate.StoreDate.AddDays(1);
            
            // Reset order number
            appDate.CurrentOrderNumber = 0;

            _unitOfWork.Repository<AppDate>().Update(appDate);
            var result = await _unitOfWork.CompleteAsync();
            return result > 0;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to Close Day");
            return false;
        }
    }
}