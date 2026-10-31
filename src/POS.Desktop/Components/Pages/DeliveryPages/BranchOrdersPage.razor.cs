using BlazorBase.ERPFrontServices.BranchServices;
using BlazorBase.ERPFrontServices.ReportingServices;
using BlazorBase.Components.Shared;
using POS.Desktop.Components.DistributionComponents;
using POS.Desktop.Components.Pages.SummaryPages;

namespace POS.Desktop.Components.Pages.DeliveryPages;

public partial class BranchOrdersPage : IDisposable
{
    [Inject] private IAuthorizationService    AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider  { get; set; } = default!;
    [Inject] private HandelDeliveryInvocation    DeliveryInvocation { get; set; } = default!;

    // ── State ──────────────────────────────────────────────────────────
    private List<OrderDto>           _allOrders   = new();
    private List<BranchToReturnDto>  _branches    = new();
    private BranchToReturnDto?       _selectedBranch;
    private bool                     _isLoading;
    private string                   _searchText  = string.Empty;

    // Nullable date for MudDatePicker binding
    private DateTime? _selectedDateNullable;
    private DateTime  SelectedDate => _selectedDateNullable ?? DateTime.Today;

    // ── Permissions ────────────────────────────────────────────────────
    private bool _canViewOrder;
    private bool _canPrintCustomer;
    private bool _canPrintDelivery;
    private bool _canVoidOrder;

    private bool _isArabic => Localizer.GetCurrentLanguage() == "ar";

    // ── Computed / Filtered ────────────────────────────────────────────
    private IEnumerable<OrderDto> FilteredOrders
    {
        get
        {
            if (_selectedBranch == null) return Enumerable.Empty<OrderDto>();

            var q = _allOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var s = _searchText.ToLower();
                q = q.Where(o =>
                    (o.OrderId.ToString().Contains(s)) ||
                    (o.CustomerName?.ToLower().Contains(s) ?? false) ||
                    (o.Phone1?.Contains(s) ?? false) ||
                    (o.ZoneName?.ToLower().Contains(s) ?? false));
            }

            return q;
        }
    }

    // ── Lifecycle ──────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        // Set date from app date service
        var appDate = await AppDateService.GetAppDate();
        _selectedDateNullable = appDate.PosDate;

        // Load permissions strictly without fallback
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            _canViewOrder     = (await AuthorizationService.AuthorizeAsync(user, "CanViewBranchDeliveryOrderDetails")).Succeeded;
            _canPrintCustomer = (await AuthorizationService.AuthorizeAsync(user, "CanPrintBranchCustomerReceipt")).Succeeded;
            _canPrintDelivery = (await AuthorizationService.AuthorizeAsync(user, "CanPrintBranchDeliveryReceipt")).Succeeded;
            _canVoidOrder     = (await AuthorizationService.AuthorizeAsync(user, "CanVoidBranchDeliveryOrder")).Succeeded;
        }

        // Load branch list
        try
        {
            var branches = await BranchService.GetBranches();
            _branches = branches?.ToList() ?? new();

            // Pre-select current branch if available
            if (_commonProperties.BranchDetails != null)
                _selectedBranch = _branches.FirstOrDefault(b => b.Id == _commonProperties.BranchDetails.Id)
                               ?? _branches.FirstOrDefault();
            else
                _selectedBranch = _branches.FirstOrDefault();

            if (_selectedBranch != null)
                await LoadOrders();
        }
        catch (Exception ex)
        {
            Snackbar.Add((_isArabic ? "خطأ في تحميل الفروع: " : "Error loading branches: ") + ex.Message, Severity.Error);
        }

        // Subscribe to new order notifications for real-time list updates
        DeliveryInvocation.OnNewOrderReceived += OnNewOrderReceived;
    }

    private async void OnNewOrderReceived()
    {
        await InvokeAsync(async () => 
        {
            // Only refresh if we are currently looking at today's orders
            if (SelectedDate == DateTime.Today)
            {
                await LoadOrders();
                StateHasChanged();
            }
        });
    }

    public void Dispose()
    {
        // Unsubscribe to avoid memory leaks
        DeliveryInvocation.OnNewOrderReceived -= OnNewOrderReceived;
    }

    // ── Branch changed ─────────────────────────────────────────────────
    private async Task OnBranchChanged(BranchToReturnDto? branch)
    {
        _selectedBranch = branch;
        await LoadOrders();
    }

    // ── Load orders ────────────────────────────────────────────────────
    private async Task LoadOrders()
    {
        if (_selectedBranch == null) return;

        _isLoading = true;
        try
        {
            // Fetch all delivery orders for the selected date
            var all = await ReportingService.GetTodayOrders(SelectedDate, "Delivery");

            // Filter by branch id on the client side
            _allOrders = all
                .Where(o => o.BranchId == _selectedBranch.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add((_isArabic ? "خطأ: " : "Error: ") + ex.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────
    private void GoBack() => _navigationManager.NavigateTo("/delivery");

    private Color GetStatusColor(string? status) => status switch
    {
        "Completed"               => Color.Success,
        "Voided"                  => Color.Error,
        "Pending"                 => Color.Primary,
        "Dispatched"              => Color.Warning,
        "FailedToDeliverToBranch" => Color.Error,
        "SentToBranch"            => Color.Info,
        _                         => Color.Default
    };

    private string GetStatusAr(string? status) => status switch
    {
        "Completed"               => "مكتمل",
        "Voided"                  => "ملغي",
        "Pending"                 => "قيد التنفيذ",
        "Dispatched"              => "مُرسل",
        "FailedToDeliverToBranch" => "فشل الإرسال للفرع",
        "SentToBranch"            => "تم الإرسال للفرع",
        _                         => status ?? ""
    };

    // ── Actions ────────────────────────────────────────────────────────

    private async Task ViewOrder(OrderDto order)
    {
        var parameters = new DialogParameters { ["Order"] = order };
        var options    = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Large, FullWidth = true };
        await DialogService.ShowAsync<DeliveryOrderViewDialog>(
            _isArabic ? "تفاصيل الأوردر" : "Order Details", parameters, options);
    }

    private async Task PrintCustomerReceipt(OrderDto order)
    {
        try
        {
            var parameters = new DialogParameters { ["Order"] = order, ["PreviewMode"] = "Customer" };
            var options    = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            await DialogService.ShowAsync<OrderReceiptPreviewDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            Snackbar.Add((_isArabic ? "خطأ في الطباعة: " : "Print error: ") + ex.Message, Severity.Error);
        }
    }

    private async Task PrintDeliveryReceipt(OrderDto order)
    {
        try
        {
            var parameters = new DialogParameters { ["Order"] = order, ["PreviewMode"] = "Kitchen" };
            var options    = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            await DialogService.ShowAsync<OrderReceiptPreviewDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            Snackbar.Add((_isArabic ? "خطأ في الطباعة: " : "Print error: ") + ex.Message, Severity.Error);
        }
    }

    private async Task VoidOrder(OrderDto order)
    {
        var parameters = new DialogParameters
        {
            ["OrderId"]                  = order.Id,
            ["IsDineIn"]                 = false,
            ["IsDistribution"]           = true,
            ["InitialDistributionOrder"] = order
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = false };
        var dialog  = await DialogService.ShowAsync<VoidOrderDialog>(
            _isArabic ? "إلغاء الأوردر" : "Void Order", parameters, options);
        var result  = await dialog.Result;

        if (result != null && !result.Canceled)
            await LoadOrders();
    }

    private async Task ResendOrder(OrderDto order)
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            var success = await ReportingService.ResendOrderToBranch(order.Id);
            if (success)
            {
                Snackbar.Add(_isArabic ? "تم إعادة إرسال الأوردر بنجاح" : "Order resent successfully", Severity.Success);
                await LoadOrders();
            }
            else
            {
                Snackbar.Add(_isArabic ? "فشل إعادة الإرسال" : "Failed to resend order", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add((_isArabic ? "خطأ: " : "Error: ") + ex.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnDateChanged(DateTime? newDate)
    {
        _selectedDateNullable = newDate;
        await LoadOrders();
    }
}
