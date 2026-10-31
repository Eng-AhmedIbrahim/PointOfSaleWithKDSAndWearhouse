namespace BlazorBase.Models;

public class OrderDetails
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateTime? OrderDataTime { get; set; }
    public string? CashierName { get; set; }
    public string? OrderType { get; set; }
    public List<TableItem> Items { get; set; } = new List<TableItem>();
    public bool? IsVoided { get; set; }

    // Hospitality & Staff Meals
    public bool IsHospitality { get; set; }
    public string? HospitalityResponsibleId { get; set; }
    public string? HospitalityResponsibleName { get; set; }
    public string? HospitalityReason { get; set; }
    public bool IsStaffMeal { get; set; }
    public string? StaffMealEmployeeId { get; set; }
    public string? StaffMealEmployeeName { get; set; }

    public decimal? Account { get; set; }
    public OrderDiscount OrderDiscount { get; set; } = new();

    public decimal? Tax { get; set; }
    public decimal? Service { get; set; }
    public decimal? Total { get; set; }
    public string? OrderNote { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? MachineName { get; set; }
    public decimal? CaptainTipsDeduction { get; set; }
    public decimal? Paid { get; set; }
    public decimal? Remain { get; set; }

    public OrderDetails Clone()
    {
        return new OrderDetails
        {
            OrderDataTime = this.OrderDataTime,
            CashierName = this.CashierName,
            OrderType = this.OrderType,
            Account = this.Account,
            OrderDiscount = this.OrderDiscount,
            Id = this.Id,
            OrderId = this.OrderId,
            Tax = this.Tax,
            Service = this.Service,
            Total = this.Total,
            OrderNote = this.OrderNote,
            CustomerName = this.CustomerName,
            CustomerPhone = this.CustomerPhone,
            PaymentMethod = this.PaymentMethod,
            MachineName = this.MachineName,
            CaptainTipsDeduction = this.CaptainTipsDeduction,
            Paid = this.Paid,
            Remain = this.Remain,
            Items = this.Items.Select(item => item.Clone()).ToList() // Deep copy items
        };
    }

    public void Merge(OrderDetails other)
    {
        if (other == null) return;

        Items.AddRange(other.Items);

        Account = (Account ?? 0) + (other.Account ?? 0);
        Tax = (Tax ?? 0) + (other.Tax ?? 0);
        Service = (Service ?? 0) + (other.Service ?? 0);
        Total = (Total ?? 0) + (other.Total ?? 0);

    }

}
