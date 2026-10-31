namespace POS.Contract.Dtos.ItemDto;

public class MenuSalesItemAttributes
{
    public int AppearanceIndex { get; set; }
    public List<MenuSalesItemsGroupDto> GroupItems { get; set; } = [];

    public MenuSalesItemAttributes Clone()
    {
        return new MenuSalesItemAttributes
        {
            AppearanceIndex = this.AppearanceIndex,
            GroupItems = this.GroupItems?.Select(group => group.Clone()).ToList() ?? new List<MenuSalesItemsGroupDto>()
        };
    }
}