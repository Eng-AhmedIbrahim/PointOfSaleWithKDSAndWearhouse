namespace POS.API.Tests;

public class AttributeItem2
{
    public int Id { get; set; }
    public int AppearanceIndex { get; set; }  // Determines the order in which items appear

    public int AttributeId { get; set; }
    public Attribute Attribute { get; set; }

    public int RelatedItemId { get; set; }  // This links to the related item // 1 , 2 , 20 , 23
    public Item RelatedItem { get; set; }
}
