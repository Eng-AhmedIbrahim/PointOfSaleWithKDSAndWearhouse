namespace POS.Core.Entities.Date;

public class AppDate : BaseEntity
{
    public int BranchId { get; set; }
    public DateTime PosDate { get; set; }
    public DateTime StoreDate { get; set; }
    public int CurrentOrderNumber { get; set; } = 1;
} 