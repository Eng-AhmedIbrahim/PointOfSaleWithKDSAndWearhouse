using System;
using System.Collections.Generic;
using POS.Contract.Models;

namespace POS.Contract.Dtos.DineIn;

public class DineInOrderDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? ShiftId { get; set; }
    public string? CashierId { get; set; }
    public string? CashierName { get; set; }
    public string? CaptainId { get; set; }
    public string? CaptainName { get; set; }
    public int TableId { get; set; }
    public string? TableName { get; set; }
    public DateTime OrderDateTime { get; set; }
    public string? OrderState { get; set; }
    public decimal? Subtotal { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Service { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountType { get; set; }
    public string? DiscountReason { get; set; }
    public decimal? DiscountedItems { get; set; }
    public decimal? TotalDiscount { get; set; }
    public decimal? GrandTotal { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? Paid { get; set; }
    public decimal? Remain { get; set; }
    public string? OrderNotice { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime? ClosedDateTime { get; set; }
    public int PrintCount { get; set; }
    public string? MachineName { get; set; }
    public decimal? CaptainTipsDeduction { get; set; }
    
    public int? CustomerCount { get; set; }
    public int? MaleCount { get; set; }
    public int? FemaleCount { get; set; }
    public DateTime? ScheduleDateTime { get; set; }
    public decimal? ReservationPaid { get; set; }
    public string? ReservationCustomerName { get; set; }
    public string? ReservationCustomerPhone { get; set; }
    
    // Hospitality & Staff Meals
    public bool IsHospitality { get; set; }
    public string? HospitalityResponsibleId { get; set; }
    public string? HospitalityResponsibleName { get; set; }
    public string? HospitalityReason { get; set; }
    public bool IsStaffMeal { get; set; }
    public string? StaffMealEmployeeId { get; set; }
    public string? StaffMealEmployeeName { get; set; }
    
    public List<OrderItemsDetailsDto>? OrderDetails { get; set; } = new();
}

public class OrderItemsDetailsDto
{
    public int Id { get; set; }
    public int? MenuSalesItemId { get; set; }
    public string? ItemName { get; set; } // Optional: for display
    public decimal? Price { get; set; } // Optional: for display
    public decimal? Quantity { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? TotalAfterDiscount { get; set; }
    public decimal? Total { get; set; }
    public string? OrderType { get; set; }
    public string? ItemNameAr { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public List<OrderItemAttributesDto>? OrderItemAttributes { get; set; } = new();
    public List<OrderItemCommentDto>? OrderItemComments { get; set; } = new();
    public string? LineComment { get; set; }
    
    // Discount properties
    public bool? Discount { get; set; }
    public decimal? TotalDiscountPrice { get; set; }
    public decimal? TotalDiscountPercentage { get; set; }
    public decimal? TotalDiscountAmount { get; set; }

    // Printing properties
    public int? ItemKitchenTypeId { get; set; }
    public int? CategoryKitchenTypeId { get; set; }
    public bool? PrintInBackupReceiptFromItem { get; set; }
    public bool? PrintInBackupReceiptFromCategory { get; set; }

    // Void details
    public bool? IsVoided { get; set; }
    public decimal? VoidAmount { get; set; }
    public decimal? TotalVoidAmount { get; set; }
    public string? VoidBy { get; set; }
    public string? VoidByName { get; set; }
    public DateTime? VoidTime { get; set; }
    public string? VoidReason { get; set; }

    // Hospitality & Staff Meals
    public bool IsHospitality { get; set; }
    public string? HospitalityResponsibleName { get; set; }
    public bool IsStaffMeal { get; set; }
    public string? StaffMealEmployeeName { get; set; }
}

public class OrderItemCommentDto
{
    public int Id { get; set; }
    public string? Comment { get; set; }
    public DateTime CommentTime { get; set; }
    public string? AddedBy { get; set; }
}

public class OrderItemAttributesDto
{
    public int Id { get; set; }
    public int? AttributeItemId { get; set; }
    public string? AttributeName { get; set; }
}

public record OrderItemSplitDto(int OrderItemDetailId, decimal QuantityToMove);
public record OrderItemVoidDto(int OrderItemDetailId, decimal QuantityToVoid);

public class SplitTargetDto
{
    public int TargetTableId { get; set; }
    public string? Label { get; set; }
    public List<OrderItemSplitDto> ItemsToMove { get; set; } = new();
}
