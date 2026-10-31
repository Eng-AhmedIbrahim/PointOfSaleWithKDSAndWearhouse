namespace ERPFront.Models;

public class DispatcherSettings
{
    public int RefreshTimeForDeliveryOrderColorsPerSecond { get; set; } = 30;
    public int CriticalTimeForDeliveryOrderPerMinute { get; set; } = 60;
    public int WarningTimeForDeliveryOrderPerMinute { get; set; } = 45;
    public int VoidLimitMinutesForDeliveryOrder { get; set; } = 15;
    public bool IsDispatcher { get; set; } = false;
    public bool AllowVoidLimitMinutesForDeliveryOrder { get; set; } = true;
    public bool AllowDeliveryVoidFromBranch { get; set; } = true;
    public bool SoundEnableCallCenter { get; set; } = true;
}
