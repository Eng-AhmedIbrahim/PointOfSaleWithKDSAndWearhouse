namespace BackOffice.Desktop.Components.Pages.Transactions;

public partial class AccountsPage
{
    [Inject] private IAuthorizationService _authorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider _authStateProvider { get; set; } = default!;
    [Inject] private IReportingErpService _reportingService { get; set; } = default!;
    [Inject] private IAppDateService _appDateService { get; set; } = default!;
    [Inject] private LocalizationService _localizer { get; set; } = default!;
    [Inject] private ISnackbar _snackbar { get; set; } = default!;
    [Inject] private IDialogService _dialogService { get; set; } = default!;

    private bool _canViewOrderDetails;
    private bool _canPrintOrderReport;
    private List<AccountSummaryDto> _summaries = new();
    private bool _isLoading = false;
    private DateTime? _selectedDate = DateTime.Today;

    protected override async Task OnInitializedAsync()
    {
        var appDate = await _appDateService.GetAppDate();
        _selectedDate = appDate.PosDate;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            _canViewOrderDetails = (await _authorizationService.AuthorizeAsync(user, "CanViewOrderAtBackOffice")).Succeeded;
            _canPrintOrderReport = (await _authorizationService.AuthorizeAsync(user, "CanPrintStaffAccountsAtBackOffice")).Succeeded;
        }

        await LoadData();
    }

    private async Task LoadData()
    {
        if (_selectedDate == null) return;
        _isLoading = true;
        try
        {
            _summaries = await _reportingService.GetAccountsSummary(_selectedDate.Value, "Cashier");
        }
        catch (Exception ex)
        {
            _snackbar.Add("Error: " + ex.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task OpenStaffDetails(AccountSummaryDto summary)
    {
        if (_selectedDate == null) return;
        
        try
        {
            var orders = await _reportingService.GetStaffOrders(_selectedDate.Value, summary.Id, summary.Type);
            
            var parameters = new DialogParameters
            {
                ["StaffId"] = summary.Id,
                ["StaffName"] = summary.Name,
                ["StaffType"] = summary.Type,
                ["SelectedDate"] = _selectedDate.Value,
                ["Orders"] = orders
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
            _dialogService.Show<StaffTransactionsDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            _snackbar.Add("Error loading details: " + ex.Message, Severity.Error);
        }
    }

    private List<ModeDetails> BuildModeDetails(List<OrderDto> allOrders)
    {
        if (allOrders == null || !allOrders.Any()) return new();
        bool isAr = _localizer.GetCurrentLanguage() == "ar";

        var types = new[] { "TakeAway", "Delivery", "DineIn", "Hospitality", "Staff" };
        var list = new List<ModeDetails>();
        foreach (var t in types)
        {
            var ords = allOrders.Where(o => string.Equals(o.OrderType, t, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!ords.Any()) continue;

            string title = t switch
            {
                "TakeAway" => isAr ? "تيك أواي" : "Takeaway",
                "Delivery" => isAr ? "دليفري" : "Delivery",
                "DineIn" => isAr ? "صالة" : "Dine-In",
                "Hospitality" => isAr ? "ضيافة" : "Hospitality",
                "Staff" => isAr ? "العاملين" : "Staff",
                _ => t
            };

            var completed = ords.Where(o => o.OrderState == "Completed").ToList();
            decimal disc = ords.Sum(o => (o.TotalOrderDiscount ?? 0) > 0 ? o.TotalOrderDiscount.Value : ((o.TotalDiscount ?? 0) > 0 ? o.TotalDiscount.Value : (o.DiscountedItems ?? 0)));
            decimal voidAmt = ords.Sum(o => o.VoidAmount ?? (o.OrderState == "Voided" ? (o.GrandTotal ?? (o.SubTotal ?? 0)) : 0));
            foreach (var o in completed)
            {
                if (o.OrderDetails != null)
                {
                    var itemVoids = o.OrderDetails.Where(i => i.IsVoided || (i.VoidAmount ?? 0) > 0).Sum(i => i.VoidAmount ?? (i.Total ?? ((i.Price ?? 0) * i.Quantity)));
                    if ((o.VoidAmount ?? 0) == 0 && itemVoids > 0) voidAmt += itemVoids;
                }
            }

            if (ords.Count == 0 && voidAmt == 0 && disc == 0) continue;

            list.Add(new ModeDetails
            {
                ModeTitle = title,
                OrderCount = ords.Count,
                Discount = disc,
                VoidAmount = voidAmt,
                GrandTotal = completed.Sum(o => o.GrandTotal ?? 0)
            });
        }
        return list;
    }

    private List<SalesItemSummaryDto> BuildSalesItems(List<OrderDto> allOrders)
    {
        if (allOrders == null || !allOrders.Any()) return new();
        return allOrders
            .SelectMany(o => o.OrderDetails ?? new List<global::POS.Contract.Models.TableItem>())
            .Where(i => !i.IsVoided && (i.VoidAmount ?? 0) == 0)
            .GroupBy(i => new { Name = i.Name ?? "صنف", Price = i.Price ?? 0 })
            .Select(g => new SalesItemSummaryDto
            {
                ItemName = g.Key.Name,
                Quantity = g.Sum(x => x.Quantity),
                UnitPrice = g.Key.Price,
                TotalAmount = g.Sum(x => x.Total ?? (x.Quantity * (x.Price ?? 0)))
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();
    }

    private string ResolveBranchName(List<OrderDto>? allOrders)
    {
        if (!string.IsNullOrWhiteSpace(_commonProperties.StoreName)) return _commonProperties.StoreName;
        if (!string.IsNullOrWhiteSpace(_commonProperties.BranchDetails?.Name)) return _commonProperties.BranchDetails.Name;
        var fromOrders = allOrders?.FirstOrDefault(o => !string.IsNullOrEmpty(o.BranchName))?.BranchName;
        if (!string.IsNullOrWhiteSpace(fromOrders)) return fromOrders;
        return _localizer.GetCurrentLanguage() == "ar" ? "الفرع الرئيسي" : "Main Branch";
    }

    private async Task PrintAccountsReport()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            var allOrders = await _reportingService.GetTodayOrders(_selectedDate ?? DateTime.Today);
            var branchName = ResolveBranchName(allOrders);
            var modeDetails = BuildModeDetails(allOrders);
            var items = BuildSalesItems(allOrders);

            decimal totalDisc = allOrders.Sum(o => (o.TotalOrderDiscount ?? 0) > 0 ? o.TotalOrderDiscount.Value : ((o.TotalDiscount ?? 0) > 0 ? o.TotalDiscount.Value : (o.DiscountedItems ?? 0)));
            decimal totalVoid = allOrders.Sum(o => o.VoidAmount ?? (o.OrderState == "Voided" ? (o.GrandTotal ?? (o.SubTotal ?? 0)) : 0));

            // Sync staff discount and void into _summaries if needed
            foreach (var s in _summaries)
            {
                var sOrders = allOrders.Where(o => o.CashierId == s.Id || o.CashierName == s.Name).ToList();
                if (s.DiscountAmount == 0)
                    s.DiscountAmount = sOrders.Sum(o => (o.TotalOrderDiscount ?? 0) > 0 ? o.TotalOrderDiscount.Value : ((o.TotalDiscount ?? 0) > 0 ? o.TotalDiscount.Value : (o.DiscountedItems ?? 0)));
                if (s.VoidAmount == 0)
                    s.VoidAmount = sOrders.Sum(o => o.VoidAmount ?? (o.OrderState == "Voided" ? (o.GrandTotal ?? (o.SubTotal ?? 0)) : 0));
            }

            var parameters = new DialogParameters
            {
                ["ReportTitle"] = _localizer.GetCurrentLanguage() == "ar" ? "تقرير حسابات الموظفين" : "Staff Accounts Report",
                ["BranchName"] = branchName,
                ["ReportDate"] = _selectedDate ?? DateTime.Today,
                ["InitialShowOrderDetails"] = false,
                ["IsStaffReport"] = true,
                ["Items"] = items,
                ["SummaryData"] = new SalesSummaryDto
                {
                    PosDate = _selectedDate ?? DateTime.Today,
                    CashierSummaries = _summaries,
                    DetailedOrders = allOrders,
                    Overall = new OverallSummaryDto
                    {
                        TotalSales = _summaries.Sum(s => s.TotalAmount),
                        CashAmount = _summaries.Sum(s => s.CashAmount),
                        CreditAmount = _summaries.Sum(s => s.CreditAmount),
                        VoidAmount = totalVoid > 0 ? totalVoid : _summaries.Sum(s => s.VoidAmount),
                        TotalDiscount = totalDisc > 0 ? totalDisc : _summaries.Sum(s => s.DiscountAmount),
                        ModeDetails = modeDetails
                    }
                }
            };
            
            var options = new DialogOptions { FullScreen = true, CloseButton = false, NoHeader = true };
            await _dialogService.ShowAsync<ReportPreviewDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            _snackbar.Add("Error preparing preview: " + ex.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task PrintDetailedAccountsReport()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            // Fetch ALL orders for the day to enable per-staff breakdown
            var allOrders = await _reportingService.GetTodayOrders(_selectedDate ?? DateTime.Today);
            var branchName = ResolveBranchName(allOrders);
            var modeDetails = BuildModeDetails(allOrders);
            var items = BuildSalesItems(allOrders);

            decimal totalDisc = allOrders.Sum(o => (o.TotalOrderDiscount ?? 0) > 0 ? o.TotalOrderDiscount.Value : ((o.TotalDiscount ?? 0) > 0 ? o.TotalDiscount.Value : (o.DiscountedItems ?? 0)));
            decimal totalVoid = allOrders.Sum(o => o.VoidAmount ?? (o.OrderState == "Voided" ? (o.GrandTotal ?? (o.SubTotal ?? 0)) : 0));

            // Sync staff discount and void into _summaries if needed
            foreach (var s in _summaries)
            {
                var sOrders = allOrders.Where(o => o.CashierId == s.Id || o.CashierName == s.Name).ToList();
                if (s.DiscountAmount == 0)
                    s.DiscountAmount = sOrders.Sum(o => (o.TotalOrderDiscount ?? 0) > 0 ? o.TotalOrderDiscount.Value : ((o.TotalDiscount ?? 0) > 0 ? o.TotalDiscount.Value : (o.DiscountedItems ?? 0)));
                if (s.VoidAmount == 0)
                    s.VoidAmount = sOrders.Sum(o => o.VoidAmount ?? (o.OrderState == "Voided" ? (o.GrandTotal ?? (o.SubTotal ?? 0)) : 0));
            }
            
            var parameters = new DialogParameters
            {
                ["ReportTitle"] = _localizer.GetCurrentLanguage() == "ar" ? "تقرير حسابات مفصل" : "Detailed Staff Report",
                ["BranchName"] = branchName,
                ["ReportDate"] = _selectedDate ?? DateTime.Today,
                ["InitialShowOrderDetails"] = true,
                ["InitialShowStaffDetails"] = true,
                ["IsStaffReport"] = true,
                ["Items"] = items,
                ["SummaryData"] = new SalesSummaryDto
                {
                    PosDate = _selectedDate ?? DateTime.Today,
                    CashierSummaries = _summaries,
                    DetailedOrders = allOrders,
                    Overall = new OverallSummaryDto
                    {
                        TotalSales = _summaries.Sum(s => s.TotalAmount),
                        CashAmount = _summaries.Sum(s => s.CashAmount),
                        CreditAmount = _summaries.Sum(s => s.CreditAmount),
                        VoidAmount = totalVoid > 0 ? totalVoid : _summaries.Sum(s => s.VoidAmount),
                        TotalDiscount = totalDisc > 0 ? totalDisc : _summaries.Sum(s => s.DiscountAmount),
                        ModeDetails = modeDetails
                    }
                }
            };
            
            var options = new DialogOptions { FullScreen = true, CloseButton = false, NoHeader = true };
            await _dialogService.ShowAsync<ReportPreviewDialog>("", parameters, options);
        }
        catch (Exception ex) { _snackbar.Add(ex.Message, Severity.Error); }
        finally { _isLoading = false; StateHasChanged(); }
    }

    private void GoBack()
    {
        _navigationManager.NavigateTo("/");
    }
}
