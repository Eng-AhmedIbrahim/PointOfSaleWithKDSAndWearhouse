namespace POS.Desktop.Components.Pages.SummaryPages;

public partial class SummaryPage
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IPrintOrderService PrintOrderService { get; set; } = default!;
    [Inject] private IBranchService BranchService { get; set; } = default!;

    private bool _canViewDetailedSales;
    private bool _canViewSalesItems;
    private bool _canPrint;
    private bool _canPosSettingsFeature;
    private SalesSummaryDto _summaryData = new();
    private bool _isLoading = true;
    private DateTime SelectedDate = DateTime.Today;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity is { IsAuthenticated: true })
        {
            _canViewDetailedSales = (await AuthorizationService.AuthorizeAsync(user, "CanViewDetailedSales")).Succeeded;
            _canViewSalesItems = (await AuthorizationService.AuthorizeAsync(user, "CanViewSalesItems")).Succeeded;
            _canPrint = (await AuthorizationService.AuthorizeAsync(user, "CanPrintSummaryReport")).Succeeded;
        }

        try
        {
            var appDate = await AppDateService.GetAppDate();
            SelectedDate = appDate.PosDate;
            _summaryData = await ReportingService.GetSalesSummary(SelectedDate);
            if (string.IsNullOrEmpty(_summaryData.Overall.Currency))
                _summaryData.Overall.Currency = "L.E";
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error loading summary: " + ex.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void GoBack()
    {
        _commonProperties.CurrentPosMode = "TakeAway";
        _navigationManager.NavigateTo("/pos");
    }

    private async Task ShowSalesItems()
    {
        try
        {
            var items = await ReportingService.GetSalesItemsSummary(SelectedDate);
            var parameters = new DialogParameters { ["Items"] = items, ["SelectedDate"] = SelectedDate };
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = false, NoHeader = true };
            await DialogService.ShowAsync<SalesItemsDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error loading sales items: " + ex.Message, Severity.Error);
        }
    }

    private async Task PrintSummary()
    {
        try
        {
            var items = await ReportingService.GetSalesItemsSummary(SelectedDate);
            var branches = await BranchService.GetBranches();
            var branchName = _commonProperties.StoreName;
            if (branches != null && branches.Any())
            {
                branchName = branches.First().Name;
            }

            var parameters = new DialogParameters
            {
                ["ReportTitle"] = Localizer["SalesSummary"],
                ["BranchName"] = branchName,
                ["ReportDate"] = SelectedDate,
                ["Items"] = items,
                ["SummaryData"] = _summaryData
            };
            
            var options = new DialogOptions { FullScreen = true, CloseButton = false, NoHeader = true };
            await DialogService.ShowAsync<ReportPreviewDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error preparing preview: " + ex.Message, Severity.Error);
        }
    }

    private async Task PrintEndDayReport()
    {
        try
        {
            var items = await ReportingService.GetSalesItemsSummary(SelectedDate);
            await PrintOrderService.PrintEndDayReportAsync(_summaryData, items, null, false, true);
            Snackbar.Add("End of day report sent to printer", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error: " + ex.Message, Severity.Error);
        }
    }

    private async Task ShowDetailedSales()
    {
        try
        {
            var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true };
            var parameters = new DialogParameters { ["SelectedDate"] = SelectedDate };
            await DialogService.ShowAsync<DetailedSalesDialog>("", parameters, options);
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error: " + ex.Message, Severity.Error);
        }
    }

    private async Task OnCloseDay()
    {
        var parameters = new DialogParameters { ["Title"] = Localizer["CloseDay"] };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<EndOfDayDialog>("", parameters, options);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data is true)
        {
            await OnInitializedAsync(); // Refresh summary for new date
        }
    }
}