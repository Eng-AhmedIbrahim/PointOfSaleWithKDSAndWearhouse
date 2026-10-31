namespace POS.Core.Entities.OrderEntity;

public class OrderSetting : BaseEntity
{
    public int BranchID { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public string? OrderStatment { get; set; }
    public decimal? Service { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Tips { get; set; }
    public int? JobID { get; set; }
    public int? CustomerReceiptCount { get; set; }
    public int? FullKitchenReceiptCount { get; set; }
    public int? SeparateReceiptCount { get; set; }
    public int? ClosingReceiptCount { get; set; }
    public bool? AddServiceToItemPrice { get; set; }
    public bool? CanCloseWithoutPrint { get; set; }
    public bool? DeductCaptainTips { get; set; }
    public decimal? CaptainTipsAmount { get; set; }
    public string? ComputerName { get; set; }
    
    // Delivery order modification permissions
    public bool? CanVoidFromBranch { get; set; } = true;
    public bool? CanVoidFromCallCenter { get; set; } = true;
    public bool? CanAddItemsFromBranch { get; set; } = true;
    public bool? CanAddItemsFromCallCenter { get; set; } = true;
}