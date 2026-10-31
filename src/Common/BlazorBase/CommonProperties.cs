using POS.Contract.Models;
using POS.Contract.Dtos.PaymentDtos;
using POS.Contract.Models.ReceiptModels;

namespace BlazorBase;

public class CommonProperties
{
    public int CurrentOrderId { get; set; } = 1;
    public double CategorySpacing { get; set; } = 4.0;
    public double CategoryPadding => CategorySpacing * 2;
    public double CategoryFontSize => CategorySpacing + 16;
    public double SalesItemsHorizontalSlider = 4;
    public double SalesItemsVerticalSlider = 4;


    public decimal? SubTotal { get; set; }
    public decimal? TotalOrderPrice { get; set; }
    public List<TableItem>? TableItems { get; set; } = [];
    public List<TableItem>? VoidedTableItems { get; set; } = [];
    public List<TableItem>? AppendedTableItems { get; set; } = [];
    public bool TableItemDisabled { get; set; } = false;

    public event Action? OnChange;
    private string _currentPosMode = "TakeAway";

    public Receipt? OrderReceipt { get; set; }
    public string? CurrentUser { get; set; }
    public string? CurrentUserId { get; set; }
    public string? StoreName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    private global::POS.Contract.Models.PaymentMethod _selectedPaymentMethod = global::POS.Contract.Models.PaymentMethod.Cash;
    public global::POS.Contract.Models.PaymentMethod SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            if (_selectedPaymentMethod != value)
            {
                _selectedPaymentMethod = value;
                PaymentMethod = value.ToString(); // Sync with string property for legacy support
                OnChange?.Invoke();
            }
        }
    }
    public string? PaymentMethod { get; set; } = "Cash";
    public List<PaymentMethodToReturnDto> AvailablePaymentMethods { get; set; } = new();
    public int SelectedPaymentMethodId { get; set; }

    public int CurrentOrderCount { get; set; }

    public string CurrentPosMode
    {
        get => _currentPosMode;
        set
        {
            if (_currentPosMode != value)
            {
                _currentPosMode = value ?? "TakeAway";
                OnChange?.Invoke();
            }
        }
    }
    public Task ClearTableItems()
    {
        TableItems?.Clear();
        VoidedTableItems?.Clear();
        return Task.CompletedTask;
    }

    public int SelectedItemCount { get; set; }

    public List<FinanceSettings>? _financeSettingsList = new();

    public WaitingQueue? WaitingQueue { get; set; } = new();
    public ClaimsPrincipal AuthUser { get; set; } = new();


    /// <Discount>
    public OrderDiscount? OrderDiscount { get; set; } = new();
    public decimal? TotalDiscount { get; set; } = 0M;
    public decimal? TotalLineDiscount { get; set; } = 0M;
    public decimal TotalAmountAfterDiscount { get; set; } = 0M;
    public decimal DiscountPercentage { get; set; } = 0M;
    public decimal DiscountValue { get; set; } = 0M;
    /// </Discount>

    // <DineIn>
    public DineInOrderDetails? CurrentDineInOrder { get; set; } = new();
    public Dictionary<int, List<DineInOrderDetails>>? DineInOrdersDetails { get; set; } = new();
    public DineInOrderValues? DineInOrderValues { get; set; } = new();
    public int TableId { get; set; }
    public bool UpdateDineInOrder { get; set; } = false;

    public DineInOrderDetails? GetActiveOrder()
    {
        if (DineInOrdersDetails == null || !DineInOrdersDetails.ContainsKey(TableId))
            return null;

        var tableOrders = DineInOrdersDetails[TableId];
        if (tableOrders == null || !tableOrders.Any())
            return null;

        if (DineInOrderValues?.OrderID > 0)
        {
            var order = tableOrders.FirstOrDefault(o => o.BasicOrderDetails?.OrderId == DineInOrderValues.OrderID);
            if (order != null) return order;
        }

        return tableOrders.FirstOrDefault();
    }
    /////////


    #region Delivery
    public CustomerDetails? CustomerDetails { get; set; } = new();
    public bool UpdateDeliveryOrder { get; set; } = false;

    #endregion
    public DateOnly? PosDate { get; set; } = new();

    public ICollection<OrderSettingToReturnDto> OrderSettings { get; set; } = new List<OrderSettingToReturnDto>();
    public List<PosFeatureSettingToReturnDto> FeatureSettings { get; set; } = new();
    public TakeAwaySettings TakeAwaySettings { get; set; } = new();
    public DineInSettings DineInSettings { get; set; } = new();
    public DeliverySettings DeliverySettings { get; set; } = new();

    public IDialogReference? DialogReference { get; set; }
    public IDialogReference? ItemCommentDialogReference { get; set; }
    public IDialogReference? ItemDiscountDialogReference { get; set; }
    public IDialogReference? OrderDiscountDialogReference { get; set; }
    public IDialogReference? CustomerInfoDialogReference { get; set; }
    public IDialogReference? PaymentMethodDialogReference { get; set; }
    public IDialogReference? QuickPaymentDialogReference { get; set; }

    public OrderDto? OrderDto { get; set; } = new();
    public BranchToReturnDto? BranchDetails { get; set; } = new();

    public string? OrderNote { get; set; }
    
    #region Staff Meals
    public global::POS.Contract.Dtos.AccountDtos.StaffMealConfigDto? CurrentStaffMeal { get; set; }
    public List<int> AllowedStaffItemIds { get; set; } = new();
    public List<int> AllowedStaffCategoryIds { get; set; } = new();
    public List<MenuSalesItemsToReturnDto> AllowedStaffMenuItems { get; set; } = new();
    public int RemainingStaffMeals { get; set; }
    public decimal RemainingStaffMealAmount { get; set; }
    public void ClearStaffMeal()
    {
        CurrentStaffMeal = null;
        AllowedStaffItemIds.Clear();
        AllowedStaffCategoryIds.Clear();
        AllowedStaffMenuItems.Clear();
        RemainingStaffMeals = 0;
        RemainingStaffMealAmount = 0;
        NotifyStateChanged();
    }
    #endregion

    #region Hospitality
    public string? HospitalityResponsiblePerson { get; set; }
    public string? HospitalityReason { get; set; }
    public bool IsHospitalityMode { get; set; } = false;
    public void ClearHospitality()
    {
        HospitalityResponsiblePerson = null;
        HospitalityReason = null;
        IsHospitalityMode = false;
        NotifyStateChanged();
    }
    #endregion

    // Distribution properties
    public Dictionary<UserToReturnDto, string> Drivers { get; set; } = new();
    public IDialogReference? ChoiceDriverDialogReference { get; set; }
    public IDialogReference? DeliveryOrderViewDialogReference { get; set; }
    public IDialogReference? DriversDialogReference { get; set; }

    #region Localization & Layout
    private string _language = "ar";
    public string Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                _language = value;
                OnChange?.Invoke();
            }
        }
    }

    private bool _isRtl = false;
    public bool IsRtl
    {
        get => _isRtl;
        set
        {
            if (_isRtl != value)
            {
                _isRtl = value;
                OnChange?.Invoke();
            }
        }
    }
    public void NotifyStateChanged() => OnChange?.Invoke();
    #endregion

    public bool IsFeatureEnabled(string featureName)
        => FeatureSettings?
        .FirstOrDefault(s => s.FeatureName == featureName)?.Value ?? true;
}