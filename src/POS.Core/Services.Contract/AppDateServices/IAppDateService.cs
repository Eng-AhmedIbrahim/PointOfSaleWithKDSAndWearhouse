namespace POS.Core.Services.Contract.AppDateServices;

public interface IAppDateService
{
    public Task<AppDate> GetAppDateAsync();
    public Task<AppDate> UpdateAppDate();
    public Task<AppDate> UpdateOrderNumber();
    public Task<bool> CheckEndOfDayStatusAsync();
    public Task<bool> CloseDayAsync();
}