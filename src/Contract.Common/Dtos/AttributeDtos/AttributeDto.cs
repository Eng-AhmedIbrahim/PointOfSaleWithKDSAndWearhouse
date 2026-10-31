namespace POS.Contract.Dtos.AttributeDtos;

public class CreateAttributeDto
{
    [Required]
    public string? EnglishName { get; set; }
    [Required]
    public string? ArabicName { get; set; }

    public ICollection<AttributeItemDto> AttributeItems { get; set; } = [];
    public ICollection<AttributeGroupDto> AttributeGroups { get; set; } = [];
}