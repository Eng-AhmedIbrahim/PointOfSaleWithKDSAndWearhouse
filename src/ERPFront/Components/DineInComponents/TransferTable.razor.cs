namespace ERPFront.Components.DineInComponents;

public partial class TransferTable
{
    private List<DineInOrderDetails> OccupiedTables = new();
    private List<TableToReturnDto> AvailableTables = new();
    private int? SelectedCurrentTable;
    private int? SelectedAvailableTable;

    protected override async Task OnInitializedAsync()
    {
        if (_commonProperties.DineInOrdersDetails != null)
        {
            var allTables = await _dineInService.GetTables();

            OccupiedTables = _commonProperties.DineInOrdersDetails.Values
                .SelectMany(x => x)
                .Where(x => !string.IsNullOrEmpty(x.RelatedTableName))
                .ToList();

            AvailableTables = allTables
                .Where(t => !OccupiedTables.Any(o => o.RelatedTableId == t.Id))
                .ToList();
        }
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

    private void TransferTables()
    {
        if (SelectedCurrentTable == null || SelectedAvailableTable == null)
        {
            _snackbar.Add("Please select both a current and an available table.", Severity.Warning);
            return;
        }

        ChangeTableDetails(SelectedCurrentTable, SelectedAvailableTable, AvailableTables.First(t => t.Id == SelectedAvailableTable).TableName ?? string.Empty);

        CloseDialog();
    }

    private void ChangeTableDetails(int? oldTableId, int? newTableId, string newTableName)
    {
        if (oldTableId == null || newTableId == null) return;

        if (_commonProperties!.DineInOrdersDetails!.TryGetValue(oldTableId ?? 0, out var orderDetails) && orderDetails != null && orderDetails.Any())
        {
            foreach (var orderDetail in orderDetails.ToList())
            {
                orderDetail.RelatedTableId = newTableId;
                orderDetail.RelatedTableName = newTableName;

                if (!_commonProperties.DineInOrdersDetails.ContainsKey(newTableId.Value))
                    _commonProperties.DineInOrdersDetails[newTableId.Value] = new List<DineInOrderDetails>();
                
                _commonProperties.DineInOrdersDetails[newTableId.Value].Add(orderDetail);
            }
            _commonProperties.DineInOrdersDetails.Remove(oldTableId ?? 0);
        }
    }

    private bool IsTransferDisabled => SelectedCurrentTable == null || SelectedAvailableTable == null;

    private void CloseDialog() => _commonProperties.DialogReference?.Close();
}