namespace POS.Core.Entities.Customer;

public class CustomerTitle : BaseEntity
{
    public int? BranchID { get; set; } = 1;
    public string? CustomerTitleName { get; set; }
}