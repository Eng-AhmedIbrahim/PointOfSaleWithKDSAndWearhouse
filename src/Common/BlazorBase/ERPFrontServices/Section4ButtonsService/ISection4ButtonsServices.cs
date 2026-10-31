namespace BlazorBase.ERPFrontServices.Section4ButtonsService;

public interface ISection4ButtonsServices
{
    public void RemoveAllItems(List<TableItem> tableItems);
    public void AddOrderToWaitingQueue(List<TableItem> tableItems);
    event Func<Task>? OnPrintRequested;
    void TriggerPrint();
}
