namespace POS.Desktop.Components.DineInComponents;

public partial class TransferTable
{
    private List<DineInOrderDetails> OccupiedTables = new();
    private List<TableToReturnDto> AvailableTables = new();
    private int? SelectedCurrentTable;
    private int? SelectedAvailableTable;

    protected override async Task OnInitializedAsync()
    {
        var allTables = await _dineInService.GetTables();

        if (_commonProperties.DineInOrdersDetails != null)
        {
            // Group by TableId to show each table only once in the source list
            OccupiedTables = _commonProperties.DineInOrdersDetails.Values
                .SelectMany(x => x)
                .Where(x => !string.IsNullOrEmpty(x.RelatedTableName))
                .GroupBy(x => x.RelatedTableId)
                .Select(g => g.First())
                .ToList();

            // Source list can also include the currently active table explicitly if not already there
            if (_commonProperties.TableId > 0 && !OccupiedTables.Any(t => t.RelatedTableId == _commonProperties.TableId))
            {
                var activeOrder = _commonProperties.GetActiveOrder();
                if (activeOrder != null) OccupiedTables.Add(activeOrder);
            }
        }

        // Available tables are those with status "Available" or "Closed" AND not currently occupied by an active order
        AvailableTables = allTables
                .Where(t => !OccupiedTables.Any(o => o.RelatedTableId == t.Id))
                .ToList();
    }

    private void OnCurrentTableChanged(int? tableId)
    {
        SelectedCurrentTable = tableId;
        StateHasChanged();
    }

    private void OnAvailableTableChanged(int? tableId)
    {
        SelectedAvailableTable = tableId;
        StateHasChanged();
    }

    [Inject] private IDineInOrderFrontService _dineInOrderServiceFront { get; set; } = default!;
    [Inject] private Section4ButtonsServices _section4ButtonsServices { get; set; } = default!; // Inject service

    private async Task TransferTables()
    {
        if (SelectedCurrentTable == null || SelectedAvailableTable == null)
        {
            _snackbar.Add("Please select both a current and an available table.", Severity.Warning);
            return;
        }

        // Logic to construct new table name if available
        string newTableName = AvailableTables.FirstOrDefault(t => t.Id == SelectedAvailableTable)?.TableName ?? string.Empty;

        var success = await ChangeTableDetails(SelectedCurrentTable, SelectedAvailableTable, newTableName);

        if (success)
        {
            _snackbar.Add("Table transferred successfully!", Severity.Success);
            
            _commonProperties.CurrentDineInOrder = null;
            _commonProperties.TableId = 0;
            
            CloseDialog();
            _section4ButtonsServices.NotifyStateChanged(); // Refresh UI without reload
        }
        else
        {
            _snackbar.Add("Failed to transfer table.", Severity.Error);
        }
    }

    private async Task<bool> ChangeTableDetails(int? oldTableId, int? newTableId, string newTableName)
    {
        if (oldTableId == null || newTableId == null) return false;

        // Since we are transferring a WHOLE table in this dialog (usually), 
        // we might move ALL orders for that table or just one?
        // The original logic seemed to assume one order per table.
        // Let's move all of them.
        
        if (_commonProperties!.DineInOrdersDetails!.TryGetValue(oldTableId ?? 0, out var orderDetails) && orderDetails != null && orderDetails.Any())
        {
            bool allSuccess = true;
            foreach (var orderDetail in orderDetails.ToList())
            {
                var result = await _dineInOrderServiceFront.TransferDineInOrderAsync(orderDetail.DatabaseId, newTableId.Value, newTableName);
                if (result)
                {
                    orderDetail.RelatedTableId = newTableId;
                    orderDetail.RelatedTableName = newTableName;
                    
                    if (!_commonProperties.DineInOrdersDetails.ContainsKey(newTableId.Value))
                        _commonProperties.DineInOrdersDetails[newTableId.Value] = new List<DineInOrderDetails>();
                    
                    _commonProperties.DineInOrdersDetails[newTableId.Value].Add(orderDetail);
                    orderDetails.Remove(orderDetail);
                }
                else allSuccess = false;
            }

            if (orderDetails.Count == 0)
                _commonProperties.DineInOrdersDetails.Remove(oldTableId ?? 0);
            
            return allSuccess;
        }
        return false;
    }

    private bool IsTransferDisabled => SelectedCurrentTable == null || SelectedAvailableTable == null;

    private void CloseDialog() => _commonProperties.DialogReference?.Close();
}