namespace POS.Contract.Dtos.AttributeDtos;

public class AttributeGroupDto
{
    public int Id { get; set; }
    public string? ArabicName { get; set; }
    public string? EnglishName { get; set; }
    public int DisplayOrder { get; set; }
    public int AttributeId { get; set; }
}
