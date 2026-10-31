namespace POS.Contract.Dtos.AttributeDtos;

public class UpdatedAttributeDto
{
    public int Id { get; set; }
    public string? EnglishName { get; set; }
    public string? ArabicName { get; set; }
    public ICollection<AttributeItemDto>? AttributeItems { get; set; } = [];
    public ICollection<AttributeGroupDto>? AttributeGroups { get; set; } = [];
}