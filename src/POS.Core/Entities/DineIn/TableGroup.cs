namespace POS.Core.Entities.DineIn;

public class TableGroup : BaseEntity
{
    public int BranchID { get; set; } = 1;
    public string? GroupName { get; set; } 
    public DateTime? CreationDate { get; set; }
    public ICollection<Table>? Tables { get; set; }
}   