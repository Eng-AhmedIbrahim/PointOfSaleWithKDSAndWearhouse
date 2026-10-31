using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using POS.Core.Entities.DineIn;

namespace POS.Core.Entities.OrderEntity;

public class OrderItemsDetails:BaseEntity
{
    public int? OrderId { get; set; }
    public OrderTypes OrderType { get; set; }

    public int? MenuSalesItemId { get; set; }
    public MenuSalesItems? MenuSalesItem { get; set; }
    public decimal? Quantity { get; set; }
    public bool? Discount { get; set; }
    public decimal? TotalDiscountPrice { get; set; }
    public decimal? TotalDiscountPercentage { get; set; }
    public decimal? TotalDiscountAmount { get; set; }
    public decimal? TotalAfterDiscount { get; set; }
    public string? DiscountReason { get; set; }
    public bool? IsVoided { get; set; }
    public decimal? VoidAmount { get; set; } // This is VoidQuantity
    public decimal? TotalVoidAmount { get; set; }
    public string? VoidBy { get; set; }
    public string? VoidByName { get; set; }
    public DateTime? VoidTime { get; set; }
    public string? VoidReason { get; set; }
    public decimal? TotalAmount { get; set; }
    
    // Denormalized item details
    public string? ItemName { get; set; }
    public string? ItemNameAr { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal? UnitPrice { get; set; }
    
    // Kitchen and printing configuration
    public int? ItemKitchenTypeId { get; set; }
    public int? CategoryKitchenTypeId { get; set; }
    public bool? PrintInBackupReceiptFromCategory { get; set; }
    public bool? PrintInBackupReceiptFromItem { get; set; }
    public bool? ByWeight { get; set; }

    // Hospitality & Staff Meals
    public bool IsHospitality { get; set; }
    public string? HospitalityResponsibleName { get; set; }
    public bool IsStaffMeal { get; set; }
    public string? StaffMealEmployeeName { get; set; }

    // Per-item comment (e.g. "بدون بصل", "حار جداً")
    public string? LineComment { get; set; }

    public Orders? Order { get; set; }
    
    public int? DineInOrderId { get; set; }
    public DineInOrder? DineInOrder { get; set; }

    public ICollection<OrderItemAttributes>? OrderItemAttributes { get; set; } = new List<OrderItemAttributes>();
    public ICollection<OrderItemComment>? OrderItemComments { get; set; } = new List<OrderItemComment>();
}
