namespace POS.Contract.Dtos.AttributeDtos;

public class AttributeItemToReturnDto
{
    public int Id { get; set; }
    public int AppearanceIndex { get; set; }
    public int AttributeId { get; set; }
    public int RelatedMenuItemId { get; set; }
    public string? ItemNameArabic { get; set; }
    public string? ItemNameEnglish { get; set; }
    public int? AttributeGroupId { get; set; }
    public decimal ExtraPrice { get; set; }
}
