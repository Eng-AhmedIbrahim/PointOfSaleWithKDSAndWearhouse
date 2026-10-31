namespace POS.Core.Entities.Kitchen;

public class KitchenType : BaseEntity
{
    public int BranchId { get; set; }
    public string? KitchenName { get; set; }

    public virtual ICollection<KitchenPrinters> KitchenPrinters { get; set; } = new List<KitchenPrinters>();

    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<Category>? Categories { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<MenuSalesItems>? Items { get; set; }
}
