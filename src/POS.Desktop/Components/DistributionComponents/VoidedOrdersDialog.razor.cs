using BlazorBase.ERPFrontServices.AppDateServices;
using BlazorBase.ERPFrontServices.BranchServices;
using BlazorBase.ERPFrontServices.VoidServices;
using POS.Contract.Dtos.VoidDtos;

namespace POS.Desktop.Components.DistributionComponents;

public partial class VoidedOrdersDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string? DefaultOrderType { get; set; }

    [Inject] IAppDateService AppDateService { get; set; } = default!;
    [Inject] IVoidErpService VoidErpService { get; set; } = default!;
    [Inject] IBranchService BranchService { get; set; } = default!;

    private List<VoidReportDto> Report { get; set; } = new();
    private bool _isLoading = false;

    private DateTime SelectedDate => _selectedDateNullable ?? DateTime.Today;
    private DateTime? _selectedDateNullable = DateTime.Today;

    private string? _filterOrderType;
    private string? _filterBranch;
    private string? _filterVoidStatus;
    private string? _searchText;

    private MudTable<VoidReportDto>? _table;

    private List<string> _allBranchNames = new();

    private IEnumerable<string> AvailableBranches
    {
        get
        {
            var branches = new HashSet<string>(_allBranchNames);
            foreach (var r in Report.Where(x => !string.IsNullOrEmpty(x.BranchName)))
            {
                branches.Add(r.BranchName!);
            }
            return branches.OrderBy(b => b).ToList();
        }
    }

    private IEnumerable<VoidReportDto> FilteredReport => Report
        .Where(r =>
        {
            if (!string.IsNullOrEmpty(_filterOrderType) && r.OrderType != _filterOrderType)
                return false;
            if (_filterOrderType == "Delivery" && !string.IsNullOrEmpty(_filterBranch) && r.BranchName != _filterBranch)
                return false;
            if (!string.IsNullOrEmpty(_filterVoidStatus))
            {
                if (_filterVoidStatus == "full" && !r.IsFullyVoided) return false;
                if (_filterVoidStatus == "partial" && r.IsFullyVoided) return false;
            }
            if (!string.IsNullOrEmpty(_searchText))
            {
                var s = _searchText.ToLower();
                return (r.CustomerName?.ToLower().Contains(s) ?? false)
                    || (r.VoidByName?.ToLower().Contains(s) ?? false)
                    || (r.VoidReason?.ToLower().Contains(s) ?? false)
                    || (r.Phone?.Contains(s) ?? false)
                    || (r.BranchName?.ToLower().Contains(s) ?? false)
                    || r.OrderId.ToString().Contains(s);
            }
            return true;
        });

    private HashSet<int> _expandedRows = new();

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(DefaultOrderType))
        {
            _filterOrderType = DefaultOrderType;
        }

        try
        {
            var appDate = await AppDateService.GetAppDate();
            if (appDate != null && appDate.PosDate != default && appDate.PosDate.Year > 2000)
            {
                _selectedDateNullable = appDate.PosDate.Date;
            }
            else
            {
                _selectedDateNullable = DateTime.Today;
            }
        }
        catch
        {
            _selectedDateNullable = DateTime.Today;
        }

        try
        {
            var branchList = await BranchService.GetBranches();
            if (branchList != null && branchList.Any())
            {
                _allBranchNames = branchList
                    .Where(b => !string.IsNullOrWhiteSpace(b.Name))
                    .Select(b => b.Name!.Trim())
                    .Distinct()
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading branches: {ex.Message}");
        }

        await LoadReport();
    }

    private async Task LoadReport()
    {
        _isLoading = true;
        _expandedRows.Clear();
        StateHasChanged();

        try
        {
            Report = await VoidErpService.GetVoidReport(SelectedDate);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading void report: {ex.Message}");
            Report = new();
            Snackbar.Add("Failed to load void report", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void ToggleRow(int orderDbId)
    {
        if (_expandedRows.Contains(orderDbId))
            _expandedRows.Remove(orderDbId);
        else
            _expandedRows.Add(orderDbId);
    }

    private bool IsExpanded(int orderDbId) => _expandedRows.Contains(orderDbId);

    private string GetRowClass(VoidReportDto item, int rowNumber)
    {
        return item.IsFullyVoided ? "row-fully-voided" : "row-partially-voided";
    }

    private Color GetOrderTypeColor(string? orderType) => orderType switch
    {
        "DineIn" => Color.Info,
        "Delivery" => Color.Warning,
        "TakeAway" => Color.Success,
        _ => Color.Default
    };

    private string GetOrderTypeIcon(string? orderType) => orderType switch
    {
        "DineIn" => Icons.Material.Filled.TableRestaurant,
        "Delivery" => Icons.Material.Filled.DeliveryDining,
        "TakeAway" => Icons.Material.Filled.ShoppingBag,
        _ => Icons.Material.Filled.ReceiptLong
    };

    private string TruncateText(string? text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return "-";
        return text.Length <= maxLen ? text : text[..maxLen] + "…";
    }

    private void Close() => MudDialog.Cancel();
}
