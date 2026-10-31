using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.Item;

public class AttributeItem : BaseEntity
{
    public int AppearanceIndex { get; set; } 
    public int AttributeId { get; set; }
    public Attributes? Attribute { get; set; }
    public int RelatedMenuItemId { get; set; } 
    public MenuSalesItems? RelatedMenuItem { get; set; }

    public int? AttributeGroupId { get; set; }
    public AttributeGroup? AttributeGroup { get; set; }
    public decimal ExtraPrice { get; set; } = 0;

    [NotMapped]
    public Guid? TempGroupUid { get; set; }
}