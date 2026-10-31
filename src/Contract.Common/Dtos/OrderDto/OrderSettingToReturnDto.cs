namespace POS.Contract.Dtos.OrderDto;

public class OrderSettingToReturnDto
{
    public int BranchID { get; set; }
    public string? OrderType { get; set; }
    public string? OrderStatment { get; set; }
    public decimal? Service { get; set; } = 0M;
    public decimal? Tax { get; set; } = 0M;
    public decimal? Tips { get; set; } = 0M;
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
    public bool? CanVoidFromBranch { get; set; } = true;
    public bool? CanVoidFromCallCenter { get; set; } = true;
    public bool? CanAddItemsFromBranch { get; set; } = true;
    public bool? CanAddItemsFromCallCenter { get; set; } = true;
}