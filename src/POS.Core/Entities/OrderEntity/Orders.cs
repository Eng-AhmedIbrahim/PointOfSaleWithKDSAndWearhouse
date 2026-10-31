using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Core.Entities.OrderEntity;

public class Orders : BaseEntity
{
    public int BranchID { get; set; }
    public int OrderID { get; set; }
    public string? BranchName { get; set; }
    public ShiftHandover? Shift { get; set; }
    public int? ShiftID { get; set; }
    public string? CashierID { get; set; }
    public string? CashierName { get; set; }
    public DateTime? OrderDate { get; set; } = new DateTime();
    public OrderTypes? OrderType { get; set; }
    public OrderStates? OrderState { get; set; }
    public string? DeliveryCompany { get; set; }
    public string? TakerID { get; set; }
    public string? TakerName { get; set; }
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
    public string? DispatchID { get; set; }
    public string? DriverID { get; set; }
    public string? DriverName { get; set; }
    public DateTime? AssignTime { get; set; }
    public DateTime? BackTime { get; set; }
    public string? CollectorID { get; set; }
    public string? CollectorName { get; set; }
    public int? TableID { get; set; }
    public string? TableName { get; set; }
    public string? TableState { get; set; }
    public string? WaiterID { get; set; }
    public string? WaiterName { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Service { get; set; }
    public decimal? DeliveryFees { get; set; }
    public decimal? Discount { get; set; }
    public decimal? DiscountedItems { get; set; }
    public decimal? FreeItems { get; set; }
    public decimal? TotalDiscount { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? GrandTotal { get; set; }
    public decimal? Paid { get; set; }
    public decimal? Remain { get; set; }
    public decimal? ReservationPaid { get; set; }
    public decimal? ReservationRemain { get; set; }
    public DateTime? ScheduleDateTime { get; set; }
    public int? CustomerCount { get; set; }
    public int? MaleCount { get; set; }
    public int? FemaleCount { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountBy { get; set; }
    public string? DiscountByName { get; set; }
    public DateTime? DiscountTime { get; set; }
    public string? DiscountReason { get; set; }
    public bool? WithoutService { get; set; }
    public bool? WithoutTax { get; set; }
    public bool? WithoutDeliveryFees { get; set; }
    public string? OrderNotice { get; set; }
    public decimal? VoidAmount { get; set; }
    public string? VoidBy { get; set; }
    public string? VoidByName { get; set; }
    public DateTime? VoidTime { get; set; }
    public string? VoidReason { get; set; }
    public decimal? VoidCount { get; set; }
    public decimal? TotalVoid { get; set; }
    public int? PrintCount { get; set; }
    public DateTime? KitchenOutTime { get; set; }
    public DateTime? PackingOutTime { get; set; }
    public DateTime? ClosingTime { get; set; }
    public int? TakeawayCustomerId { get; set; }
    public TakeawayCustomer? TakeawayCustomer { get; set; }
    public string? TakeawayCustomerName { get; set; }
    public string? TakeawayCustomerPhone { get; set; }
    public string? MachineName { get; set; }
    public decimal? CaptainTipsDeduction { get; set; }
    public ICollection<OrderItemsDetails>? OrderDetails { get; set; } = new List<OrderItemsDetails>();
    
    // Reservation link
    public int? ReservationId { get; set; }
    public POS.Core.Entities.ReservationEntity.Reservation? Reservation { get; set; }
    
    public string? ReservationCustomerName { get; set; }
    public string? ReservationCustomerPhone { get; set; }
    public int? CallCenterOrderId { get; set; }
    public int? RemoteOrderId { get; set; }
    
    // For additional items added to an existing order
    public int? ParentOrderId { get; set; }
    
    public string? DeliveryBranchUrl { get; set; }
    public string? CallCenterApiUrl { get; set; }

    // Hospitality
    public bool? IsHospitality { get; set; }
    public string? HospitalityResponsibleId { get; set; }
    public string? HospitalityResponsibleName { get; set; }
    public string? HospitalityReason { get; set; }

    // Staff Meals
    public bool? IsStaffMeal { get; set; }
    public string? StaffMealEmployeeId { get; set; }
    public string? StaffMealEmployeeName { get; set; }
}