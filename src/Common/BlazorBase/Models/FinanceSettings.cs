namespace BlazorBase.Models;

public class FinanceSettings
{
    public string Label { get; set; } = string.Empty;
    public decimal? Value { get; set; } = 0.0M;
    public bool IsEnabled { get; set; } = false;
    public bool HasSwitch { get; set; } = false;
    public bool IsTotal { get; set; } = false;
}