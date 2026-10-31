namespace POS.Core.Entities.Item;

public class Attributes : BaseEntity
{
    public string? EnglishName { get; set; }
    public string? ArabicName { get; set; }
    public ICollection<AttributeGroup> AttributeGroups { get; set; } = new HashSet<AttributeGroup>();
    public ICollection<AttributeItem> AttributeItems { get; set; } = new HashSet<AttributeItem>();
}