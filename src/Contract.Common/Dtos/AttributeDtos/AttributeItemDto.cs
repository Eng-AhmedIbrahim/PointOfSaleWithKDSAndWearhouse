namespace POS.Contract.Dtos.AttributeDtos;

public class AttributeItemDto
{
    public int AppearanceIndex { get; set; }
    public int RelatedMenuItemId { get; set; }
    public int? AttributeGroupId { get; set; }
    public decimal ExtraPrice { get; set; }
}