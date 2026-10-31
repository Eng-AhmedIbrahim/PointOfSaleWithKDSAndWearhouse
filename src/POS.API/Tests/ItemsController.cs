//namespace POS.API.Tests;

//[Route("api/[controller]")]
//[ApiController]
//public class ItemsController : ControllerBase
//{
//    private List<Item> _items = new List<Item>();
//    private Item _item;
//    private Attribute2 _attribute;
//    private List<AttributeItem2> _attributeItems;

//    // Constructor initializes dummy data
//    //public ItemsController()
//    //{
//    //    // Initialize data
//    //    MakeItems();
//    //    MakeItem();
//    //    MakeAttribute();
//    //    MakeAttributeItems();

//    //    // Link attribute to the item and assign attribute items to the attribute
//    //    _attribute.AttributeItems = _attributeItems;
//    //    _item.AttributeId = _attribute.Id;
//    //    _item.Attribute = _attribute;
//    //}

//    // Method to create a list of 10 dummy items
//    private void MakeItems()
//    {
//        for (int i = 1; i <= 10; i++)
//        {
//            _items.Add(new Item { Id = i, Name = $"Db menu Item {i}" });
//        }
//    }

//    // Method to create a single dummy item
//    private void MakeItem()
//    {
//        _item = new Item
//        {
//            Id = 15,
//            Name = "Wanted Item 15"
//        };
//    }

//    // Method to create a dummy attribute
//    //private void MakeAttribute()
//    //{
//    //    _attribute = new Attribute
//    //    {
//    //        Id = 15,
//    //        Name = "Attribute 15 for Wanted Item 15"
//    //    };
//    //}

//    // Method to create a list of dummy attribute items, each linked to an item
//    private void MakeAttributeItems()
//    {
//        _attributeItems = new List<AttributeItem2>();

//        int appearanceIndex = 1; // Start with an appearance index of 1
//        int count = 0; // Counter to keep track of how many items we've processed in each group of 3

//        foreach (var item in _items)
//        {
//            _attributeItems.Add(new AttributeItem2
//            {
//                Id = item.Id,
//                RelatedItemId = item.Id,
//                AttributeId = _attribute.Id,
//                AppearanceIndex = appearanceIndex  // Assigning the appearance index
//            });

//            count++; // Increment the counter

//            // Every 3 items, increment the appearance index
//            if (count % 3 == 0)
//            {
//                appearanceIndex++;
//            }
//        }
//    }


//    // API to get the list of items
//    [HttpGet("items")]
//    public IEnumerable<Item> GetItems()
//    {
//        return _items;
//    }

//    // API to get a specific item (Item 15 in this case)
//    [HttpGet("item")]
//    public Item GetItem()
//    {
//        return _item;
//    }

//    // API to get the attribute for the specific item (Attribute 15)
//    //[HttpGet("attribute")]
//    //public Attribute GetAttribute()
//    //{
//    //    return _attribute;
//    //}

//    //// API to get the attribute items ordered by AppearanceIndex
//    //[HttpGet("attributeitems")]
//    //public IEnumerable<IGrouping<int, AttributeItem>> GetAttributeItems()
//    //{
//    //    return _attributeItems
//    //        .OrderBy(ai => ai.AppearanceIndex)
//    //        .GroupBy(ai => ai.AppearanceIndex);
//    //}

//    [HttpGet("groupedattributeitems")]
//    public IEnumerable<GroupedAttributeItemsDto> GetGroupedAttributeItems()
//    {
//        // Group the items by AppearanceIndex and return them in the desired format
//        var groupedItems = _attributeItems
//            .GroupBy(ai => ai.AppearanceIndex)
//            .Select(group => new GroupedAttributeItemsDto
//            {
//                AppearanceIndex = group.Key,
//                AttributeItems = group.Select(ai => new AttributeItemDto
//                {
//                    Id = ai.Id,
//                    AttributeId = ai.AttributeId,
//                    RelatedItemId = ai.RelatedItemId
//                }).ToList()
//            });

//        return groupedItems;
//    }

//    [HttpGet("itemwithattributes")]
//    public ItemDto GetItemWithAttributes()
//    {
//        // Group the attribute items by AppearanceIndex
//        var groupedAttributeItems = _attributeItems
//            .GroupBy(ai => ai.AppearanceIndex)
//            .Select(group => new GroupedAttributeItemsDto
//            {
//                AppearanceIndex = group.Key,
//                AttributeItems = group.Select(ai => new AttributeItemDto
//                {
//                    Id = ai.Id,
//                    AttributeId = ai.AttributeId,
//                    RelatedItemId = ai.RelatedItemId
//                }).ToList()
//            }).ToList();

//        // Create the attribute DTO
//        var attributeDto = new AttributeDto
//        {
//            Id = _attribute.Id,
//            Name = _attribute.Name,
//            GroupedAttributeItems = groupedAttributeItems
//        };

//        // Create the item DTO
//        var itemDto = new ItemDto
//        {
//            Id = _item.Id,
//            Name = _item.Name,
//            Attribute = attributeDto
//        };

//        return itemDto;
//    }


//}

//public class AttributeItemDto
//{
//    public int Id { get; set; }
//    public int AttributeId { get; set; }
//    public int RelatedItemId { get; set; }
//}

//public class GroupedAttributeItemsDto
//{
//    public int AppearanceIndex { get; set; }  // Group key
//    public List<AttributeItemDto> AttributeItems { get; set; } = new List<AttributeItemDto>();
//}

//public class AttributeDto
//{
//    public int Id { get; set; }
//    public string Name { get; set; }
//    public List<GroupedAttributeItemsDto> GroupedAttributeItems { get; set; } = new List<GroupedAttributeItemsDto>();
//}

//public class ItemDto
//{
//    public int Id { get; set; }
//    public string Name { get; set; }
//    public AttributeDto Attribute { get; set; }
//}


