namespace POS.Core.Entities.Item;

public class ItemsClassifications : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ArabicName { get; set; }
}
