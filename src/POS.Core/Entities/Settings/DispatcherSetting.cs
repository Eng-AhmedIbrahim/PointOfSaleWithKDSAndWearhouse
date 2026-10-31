/* file: POS.Core/Entities/Settings/DispatcherSetting.cs */
namespace POS.Core.Entities.Settings;

public class DispatcherSetting : BaseEntity
{
    public int RefreshTimeForDeliveryOrderColorsPerSecond { get; set; } = 30;
    public int CriticalTimeForDeliveryOrderPerMinute { get; set; } = 60;
    public int WarningTimeForDeliveryOrderPerMinute { get; set; } = 45;
    public int VoidLimitMinutesForDeliveryOrder { get; set; } = 15;
    public bool IsDispatcher { get; set; } = false;
    public bool AllowVoidLimitMinutesForDeliveryOrder { get; set; } = false;
    public bool AllowDeliveryVoidFromBranch { get; set; } = true;
    
    // Per Terminal identification if needed
    public string? ComputerName { get; set; }
}
