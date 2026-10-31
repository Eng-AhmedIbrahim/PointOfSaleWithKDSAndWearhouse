using POS.Contract.Dtos.OrderDto;

namespace POS.Contract.Dtos.OrderDtos;

public class OrderDto
{
    public int Id { get; set; }
    //for all Orders
    public string? OrderType { get; set; }
    public string? CashierName { get; set; }
    public List<TableItem>? OrderDetails { get; set; }
    public int OrderId { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? CashierId { get; set; }
    public string? StaffName => CashierName; // Default to CashierName for generic reports
    public string? StaffId => CashierId;     // Default to CashierId for generic reports
    public string? FooterMessage { get; set; }
    public string? CustomerPhone { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? Paid { get; set; }
    public decimal? Remaining { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? Services { get; set; }
    public decimal? Tax { get; set; }
    public decimal? TotalOrderDiscount { get; set; }
    public decimal? GrandTotal { get; set; }

    public DateTime? OrderDate { get; set; }
    public string? OrderState { get; set; }
    public string? OrderNotice { get; set; }

    //discount
    public decimal? DiscountedItems { get; set; }
    public decimal? TotalDiscount { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountBy { get; set; }
    public string? DiscountByName { get; set; }
    public DateTime? DiscountTime { get; set; }
    public string? DiscountReason { get; set; }
    public bool? WithoutService { get; set; }
    public bool? WithoutTax { get; set; }

    public ICollection<OrderSettingToReturnDto>? OrderSettings { get; set; }

    //for takeaway
    public int? TakeawayCustomerId { get; set; }
    public string? TakeAwayCustomerName { get; set; }
    public string? TakeawayCustomerPhone { get; set; }

    //for dinein

    public string? TakerID { get; set; }
    public string? TakerName { get; set; }
    public decimal? ReservationPaid { get; set; }
    public decimal? ReservationRemain { get; set; }
    public DateTime? ScheduleDateTime { get; set; }
    public int? CustomerCount { get; set; }
    public int? TableId { get; set; }
    public string? TableName { get; set; }
    public string? WaiterId { get; set; }
    public string? WaiterName { get; set; }


    //for delivery
    public string? DeliveryCompany { get; set; }
    public string? TitleName { get; set; }
    public int? CustomerID { get; set; }
    public string? CustomerName { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? HomeNum { get; set; }
    public string? StreetName { get; set; }
    public string? FloorNum { get; set; }
    public string? ApartmentNum { get; set; }
    public string? AddressNotice { get; set; }
    public int? ZoneID { get; set; }
    public string? ZoneName { get; set; }
    public decimal? ZoneBonus { get; set; }
    public decimal? DeliveryFees { get; set; }
    public string? DispatchID { get; set; }
    public string? DriverID { get; set; }
    public string? DriverName { get; set; }
    public DateTime? AssignTime { get; set; }
    public DateTime? BackTime { get; set; }
    public string? CollectorID { get; set; }
    public string? CollectorName { get; set; }
    public bool? WithoutDeliveryFees { get; set; }
    public DateTime? ClosingTime { get; set; }
    public string? DeliveryBranchUrl { get; set; }


    //Void Details 
    public decimal? VoidAmount { get; set; }
    public string? VoidBy { get; set; }
    public string? VoidByName { get; set; }
    public DateTime? VoidTime { get; set; }
    public string? VoidReason { get; set; }
    public decimal? VoidCount { get; set; }
    public decimal? TotalVoid { get; set; }

    //Print Details 
    public int? PrintCount { get; set; }


    //Order Times 
    public DateTime? KitchenOutTime { get; set; }
    public DateTime? PackingOutTime { get; set; }
    public bool? SkipPrintingOnServer { get; set; }
    public string? MachineName { get; set; }
    public decimal? CaptainTipsDeduction { get; set; }
    public int? CallCenterOrderId { get; set; }
    public int? RemoteOrderId { get; set; }
    public string? CallCenterApiUrl { get; set; }
    public int? ParentOrderId { get; set; }

    // Hospitality
    public bool? IsHospitality { get; set; }
    public string? HospitalityResponsibleId { get; set; }
    public string? HospitalityResponsibleName { get; set; }
    public string? HospitalityReason { get; set; }

    // Staff Meals
    public bool? IsStaffMeal { get; set; }
    public string? StaffMealEmployeeId { get; set; }
    public string? StaffMealEmployeeName { get; set; }
    // Follow-up / Added items for Desktop printing
    public List<TableItem>? AddedItems { get; set; }
    public bool? IsFollowUp { get; set; }
    public List<TableItem>? VoidedItems { get; set; }
}
