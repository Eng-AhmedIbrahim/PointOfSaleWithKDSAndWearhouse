namespace BlazorBase.Models;

public class WaitingOrder
{
    public int Id { get; set; } = 1;
    public List<TableItem> Items { get; set; } = [];
}