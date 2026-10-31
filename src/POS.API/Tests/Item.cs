namespace POS.API.Tests;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }

    public int? AttributeId { get; set; } 
    public Attribute? Attribute { get; set; }  // An item can have an attribute

}
