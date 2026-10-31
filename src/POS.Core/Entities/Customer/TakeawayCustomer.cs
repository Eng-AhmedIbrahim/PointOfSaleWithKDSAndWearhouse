namespace POS.Core.Entities.Customer;

public class TakeawayCustomer:BaseEntity
{
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public ICollection<Orders>? Orders { get; set; }
}