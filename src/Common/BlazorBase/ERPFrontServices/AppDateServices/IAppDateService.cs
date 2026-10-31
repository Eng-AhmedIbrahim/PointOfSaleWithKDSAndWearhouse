namespace BlazorBase.ERPFrontServices.AppDateServices;

public interface IAppDateService
{
    public Task<AppDateToReturnDto> GetAppDate();
    public Task<AppDateToReturnDto> UpdateOrderCount();
    public Task<bool> CloseDay();
}