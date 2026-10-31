/* file: Contract.Common/Dtos/SettingsDtos/DispatcherSettingsDto.cs */
namespace POS.Contract.Dtos.SettingsDtos;

public class DispatcherSettingsDto
{
    public int Id { get; set; }
    public int RefreshTimeForDeliveryOrderColorsPerSecond { get; set; } = 30;
    public int CriticalTimeForDeliveryOrderPerMinute { get; set; } = 60;
    public int WarningTimeForDeliveryOrderPerMinute { get; set; } = 45;
    public int VoidLimitMinutesForDeliveryOrder { get; set; } = 15;
    public bool IsDispatcher { get; set; } = false;
    public bool AllowVoidLimitMinutesForDeliveryOrder { get; set; } = false;
    public bool AllowDeliveryVoidFromBranch { get; set; } = true;
    public string? ComputerName { get; set; }
}
