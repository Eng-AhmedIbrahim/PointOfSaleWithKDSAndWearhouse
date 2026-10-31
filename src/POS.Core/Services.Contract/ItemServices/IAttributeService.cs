namespace POS.Core.Services.Contract.ItemServices;

public interface IAttributeService
{
    public Task<Attributes?> CreateAttributeAsync(Attributes attribute);
    public Task<Attributes?> UpdateAttributeAsync(Attributes oldAttribute, Attributes newAttribute);
    public Task<Attributes?> GetAttributeByIdAsync(int attributeId);
    public Task<Attributes?> GetAttributeByIdWithSpecAsync(ISpecifications<Attributes> attributeSpecifications);
    public Task<bool> DeleteAttribute(Attributes attribute);
    public Task<IReadOnlyList<Attributes>?> GetAllAttributeAsync();
    public Task<IReadOnlyList<Attributes>?> GetAllAttributeWithSpecsAsync(ISpecifications<Attributes> attributeSpecifications);
    public Task<ICollection<AttributeItem>?> AddAttributeItems(ICollection<AttributeItem> attributeItem);
    public Task<bool> DeleteAttributeItem(AttributeItem attributeItem);
    public Task<AttributeItem?> GetAttributeItemByIdAsync(int attributeItemId);
}