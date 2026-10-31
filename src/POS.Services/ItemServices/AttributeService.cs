namespace POS.Services.ItemServices;

public class AttributeService : IAttributeService
{
    private readonly IUnitOfWork _unitOfWork;

    public AttributeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Attributes?> CreateAttributeAsync(Attributes attribute)
    {
        try
        {
            if (attribute is null)
                return null;

            // 1. Handle Manual ID for Attributes parent
            var existingAttributes = await _unitOfWork.Repository<Attributes>().GetAllAsync();
            int nextAttrId = (existingAttributes != null && existingAttributes.Any()) 
                ? existingAttributes.Max(attr => attr.Id) + 1 
                : 1;
            attribute.Id = nextAttrId;

            // 2. Handle Manual ID for AttributeItem children (Identity was removed from DB)
            var allAttrItems = await _unitOfWork.Repository<AttributeItem>().GetAllAsync();
            int nextItemId = (allAttrItems != null && allAttrItems.Any())
                ? allAttrItems.Max(x => x.Id) + 1
                : 1;

            foreach (var item in attribute.AttributeItems)
            {
                item.Id = nextItemId++;
                item.AttributeId = attribute.Id; 

                // If the item is linked to a group by reference (new group)
                if (item.AttributeGroup != null && (item.AttributeGroupId == null || item.AttributeGroupId == 0))
                {
                    // Find the actual group in the collection that matches the reference or DisplayOrder
                    var actualGroup = attribute.AttributeGroups.FirstOrDefault(g => 
                        g == item.AttributeGroup || 
                        (g.DisplayOrder == item.AttributeGroup.DisplayOrder && g.ArabicName == item.AttributeGroup.ArabicName));
                    
                    if (actualGroup != null)
                    {
                        item.AttributeGroup = actualGroup; // Establish relationship for EF to resolve FK
                    }
                }
            }

            // Note: AttributeGroups STILL has Identity in DB, so we leave Id=0 for them.

            await _unitOfWork.Repository<Attributes>().AddAsync(attribute);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;

            return attribute;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating Attribute.");
            return null;
        }
    }
    public async Task<bool> DeleteAttribute(Attributes attribute)
    {
        try
        {
            if (attribute is null)
                return false;

            _unitOfWork.Repository<Attributes>().Delete(attribute);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return false;

            return true;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cant Delete Attribute With Id {attributeId}", attribute.Id);
            return false;
        }
    }
    public async Task<IReadOnlyList<Attributes>?> GetAllAttributeAsync()
    {
        try
        {
            var attributes = await _unitOfWork.Repository<Attributes>().GetAllAsync();

            if (attributes is null)
                return null;

            return attributes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Attributes");
            return null;
        }
    }
    public async Task<Attributes?> GetAttributeByIdAsync(int attributeId)
    {
        try
        {
            var attribute = await _unitOfWork.Repository<Attributes>().GetByIdAsync(attributeId);

            if (attribute is null)
                return null;

            return attribute;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not Attribute With This Id {attributeId}", attributeId);
            return null;
        }
    }
    public async Task<Attributes?> UpdateAttributeAsync(Attributes oldAttribute, Attributes newAttribute)
    {
        try
        {
            // 1. Fetch tracked version with all inclusions
            var spec = new POS.Core.Specifications.AttributeSpecs.AttributeWithIncludeSpecs(newAttribute.Id);
            var trackedAttribute = await _unitOfWork.Repository<Attributes>().GetByIdWithSpecificationTrackedAsync(spec);

            if (trackedAttribute == null)
            {
                Log.Warning("Attribute with Id {id} not found for update", newAttribute.Id);
                return null;
            }

            // 2. Update basic info
            trackedAttribute.ArabicName = newAttribute.ArabicName;
            trackedAttribute.EnglishName = newAttribute.EnglishName;

            // 3. Sync AttributeGroups (Identity based)
            var groupsToRemove = trackedAttribute.AttributeGroups
                .Where(oldG => !newAttribute.AttributeGroups.Any(newG => newG.Id > 0 && newG.Id == oldG.Id))
                .ToList();
            foreach (var g in groupsToRemove) trackedAttribute.AttributeGroups.Remove(g);

            foreach (var newG in newAttribute.AttributeGroups)
            {
                if (newG.Id == 0)
                {
                    trackedAttribute.AttributeGroups.Add(newG);
                }
                else
                {
                    var existingG = trackedAttribute.AttributeGroups.FirstOrDefault(g => g.Id == newG.Id);
                    if (existingG != null)
                    {
                        existingG.ArabicName = newG.ArabicName;
                        existingG.EnglishName = newG.EnglishName;
                        existingG.DisplayOrder = newG.DisplayOrder;
                        existingG.Uid = newG.Uid;
                    }
                }
            }

            // 4. Sync AttributeItems (Manual ID based)
            trackedAttribute.AttributeItems.Clear();

            var allAttrItems = await _unitOfWork.Repository<AttributeItem>().GetAllAsync();
            int nextItemId = (allAttrItems != null && allAttrItems.Any())
                ? allAttrItems.Max(x => x.Id) + 1
                : 1;

            foreach (var item in newAttribute.AttributeItems)
            {
                var newItem = new AttributeItem
                {
                    Id = nextItemId++,
                    AttributeId = trackedAttribute.Id,
                    AppearanceIndex = item.AppearanceIndex,
                    RelatedMenuItemId = item.RelatedMenuItemId,
                    ExtraPrice = item.ExtraPrice,
                    TempGroupUid = item.TempGroupUid
                };

                if (newItem.TempGroupUid.HasValue)
                {
                    newItem.AttributeGroup = trackedAttribute.AttributeGroups
                        .FirstOrDefault(g => g.Uid == newItem.TempGroupUid);
                }

                trackedAttribute.AttributeItems.Add(newItem);
                await _unitOfWork.Repository<AttributeItem>().AddAsync(newItem);
            }

            await _unitOfWork.CompleteAsync();
            return trackedAttribute;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Occur During Update Attribute That Have Id {attributeId}", newAttribute.Id);
            return null;
        }
    }
    public async Task<ICollection<AttributeItem>?> AddAttributeItems(ICollection<AttributeItem> attributeItems)
    {
        try
        {
            if (attributeItems is null)
                return null;

            await _unitOfWork.Repository<AttributeItem>().AddRangeAsync(attributeItems);
            return attributeItems;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error Occur During Update AttributeItems");
            return null;
        }
    }
    public async Task<Attributes?> GetAttributeByIdWithSpecAsync(ISpecifications<Attributes> attributeSpecifications)
    {
        try
        {
            var attribute = await _unitOfWork.Repository<Attributes>().GetByIdWithSpecificationAsync(attributeSpecifications);

            if (attribute is null)
                return null;

            return attribute;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are No Attribute Has This Id");
            return null;
        }
    }
    public async Task<IReadOnlyList<Attributes>?> GetAllAttributeWithSpecsAsync(ISpecifications<Attributes> attributeSpecifications)
    {
        try
        {
            var attributes = await _unitOfWork.Repository<Attributes>().GetAllWithSpecificationAsync(attributeSpecifications);
            if (attributes is null)
                return null;

            return attributes;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are No Attributes");
            return null;
        }
    }
    public async Task<bool> DeleteAttributeItem(AttributeItem attributeItem)
    {
        try
        {
            if (attributeItem is null)
                return false;

            _unitOfWork.Repository<AttributeItem>().Delete(attributeItem);

            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return false;

            return true;

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cant Delete AttributeItem With Id {attributeId}", attributeItem.Id);
            return false;
        }
    }

    public async Task<AttributeItem?> GetAttributeItemByIdAsync(int attributeItemId)
    {
        try
        {
            var attribute = await _unitOfWork.Repository<AttributeItem>().GetByIdAsync(attributeItemId);

            if (attribute is null)
                return null;

            return attribute;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "There Are Not AttributeItem With This Id {attributeItemId}", attributeItemId);
            return null;
        }
    }
}