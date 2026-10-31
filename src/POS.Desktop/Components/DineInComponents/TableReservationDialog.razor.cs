namespace POS.Desktop.Components.DineInComponents;

public partial class TableReservationDialog
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int TableId { get; set; }
    [Parameter] public string TableName { get; set; } = string.Empty;
    [Parameter] public string CurrentUser { get; set; } = string.Empty;
    [Parameter] public string CurrentUserId { get; set; } = string.Empty;
    [Parameter] public int BranchId { get; set; }

    private string CustomerName { get; set; } = string.Empty;
    private string CustomerPhone { get; set; } = string.Empty;
    private DateTime? ReservationDate { get; set; } = DateTime.Today;
    private TimeSpan? ReservationTime { get; set; } = new TimeSpan(18, 0, 0); // Default 6:00 PM
    private int GuestCount { get; set; } = 2;
    private decimal DepositAmount { get; set; } = 0;
    private string SpecialRequests { get; set; } = string.Empty;

    private void Cancel() => MudDialog.Cancel();

    private async Task Reserve()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            Snackbar.Add(Localizar["CustomerNameRequired"], Severity.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(CustomerPhone))
        {
            Snackbar.Add(Localizar["PhoneNumberRequired"], Severity.Error);
            return;
        }

        if (!ReservationDate.HasValue || !ReservationTime.HasValue)
        {
            Snackbar.Add(Localizar["ReservationDateTimeRequired"], Severity.Error);
            return;
        }

        if (GuestCount < 1)
        {
            Snackbar.Add(Localizar["GuestCountAtLeastOne"], Severity.Error);
            return;
        }

        // Combine date and time
        var scheduledDateTime = ReservationDate.Value.Date.Add(ReservationTime.Value);

        // Check if reservation is in the past
        if (scheduledDateTime < DateTime.Now)
        {
            Snackbar.Add(Localizar["ReservationInPast"], Severity.Error);
            return;
        }

        // Create reservation DTO
        var reservation = new DineInOrderDto
        {
            TableId = TableId,
            TableName = TableName,
            ReservationCustomerName = CustomerName,
            ReservationCustomerPhone = CustomerPhone,
            ScheduleDateTime = scheduledDateTime,
            CustomerCount = GuestCount,
            ReservationPaid = DepositAmount,
            OrderNotice = SpecialRequests,
            CashierId = CurrentUserId,
            CashierName = CurrentUser,
            BranchId = BranchId,
            OrderState = "Reserved"
        };

        var result = await DineInOrderFrontService.ReserveTableAsync(TableId, reservation);
        
        if (result)
        {
            Snackbar.Add(string.Format(Localizar["ReservationSuccess"], TableName, CustomerName), Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add(Localizar["ReservationFailed"], Severity.Error);
        }
    }
}
