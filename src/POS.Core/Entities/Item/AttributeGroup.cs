using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.Item;

public class AttributeGroup : BaseEntity
{
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public int DisplayOrder { get; set; }
    
    public int AttributeId { get; set; }
    [ForeignKey("AttributeId")]
    public Attributes? Attribute { get; set; }
    
    public ICollection<AttributeItem> AttributeItems { get; set; } = new HashSet<AttributeItem>();

    public Guid Uid { get; set; } = Guid.NewGuid();
}
