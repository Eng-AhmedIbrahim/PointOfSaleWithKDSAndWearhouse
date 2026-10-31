namespace POS.Core.Entities.OrderEntity;

public class PosFeatureSetting : BaseEntity
{
    public string FeatureName { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool Value { get; set; }
    public string? ComputerName { get; set; }
    public string? ModuleName { get; set; }
    public string? ScreenName { get; set; }
}
