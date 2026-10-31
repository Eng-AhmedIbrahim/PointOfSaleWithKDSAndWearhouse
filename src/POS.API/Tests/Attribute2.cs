namespace POS.API.Tests;

public class Attribute2
{
    public int Id { get; set; }
    public string Name { get; set; }

    // Removed RelatedItemId/RelatedItem since Attribute belongs to an Item
    public ICollection<AttributeItem2> AttributeItems { get; set; } = new HashSet<AttributeItem2>();
}
