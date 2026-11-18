namespace POS.Core.Entities.Warehouse;

public enum RequisitionStatus
{
    PendingSync = 0,           // معلق محلياً (في انتظار اتصال سيرفر المخزن)
    Draft = 1,                 // مسودة في الفرع
    Submitted = 2,             // تم إرسال الطلب للمخزن
    UnderReview = 3,           // قيد المراجعة في المخزن
    ModificationProposed = 4,  // المخزن اقترح تعديل كميات لنقص الخامات
    Approved = 5,              // تم الاعتماد والموافقة النهائية
    Rerouted = 6,              // تم تحويل الطلب لمخزن بديل
    Preparing = 7,             // جاري تجهيز الطلبية وتعبئتها
    InTransit = 8,             // تم الشحن وفي الطريق للفرع
    Received = 9,              // الفرع استلم وأكد الاستلام
    Rejected = 10,             // تم الرفض مع ذكر السبب
    Cancelled = 11             // ملغي
}

public class BranchRequisition : BaseEntity
{
    public string RequisitionNumber { get; set; } = string.Empty;
    public int RequestingBranchId { get; set; }
    public POS.Core.Entities.Company.Branch? RequestingBranch { get; set; }

    public int TargetWarehouseId { get; set; }
    public Warehouse? TargetWarehouse { get; set; }

    public int? ReroutedToWarehouseId { get; set; }
    public Warehouse? ReroutedToWarehouse { get; set; }
    public string? RerouteReason { get; set; }

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
    public bool IsReturn { get; set; } = false; // True = مرتجع من الفرع إلى المخزن الرئيسي
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequiredDate { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByUserId { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string? DispatchedByUserId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? ReceivedByUserId { get; set; }

    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }

    // Optimistic Concurrency - يمنع التعديل المتزامن من طرفين
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<BranchRequisitionLine> Lines { get; set; } = new List<BranchRequisitionLine>();
    public ICollection<RequisitionLog> Logs { get; set; } = new List<RequisitionLog>();
}

/// <summary>سجل أحداث الطلبية - كل تغيير يُسجَّل هنا</summary>
public class RequisitionLog : BaseEntity
{
    public int RequisitionId { get; set; }
    public BranchRequisition? Requisition { get; set; }

    // Created | Submitted | UnderReview | Approved | Preparing | Dispatched
    // Received | Rejected | Cancelled | Edited | ProposalSent | ProposalAccepted | ProposalRejected | Rerouted
    public string EventType { get; set; } = string.Empty;

    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? Description { get; set; }

    public string? ChangedByUserId { get; set; }
    public string? ChangedByName { get; set; }
    public string? ChangedByRole { get; set; }  // "Branch" or "Warehouse"

    public DateTime EventDate { get; set; } = DateTime.UtcNow;
}

public class BranchRequisitionLine : BaseEntity
{
    public int RequisitionId { get; set; }
    public BranchRequisition? Requisition { get; set; }

    public int ProductId { get; set; }
    public WarehouseProduct? Product { get; set; }

    public int UnitId { get; set; }
    public WarehouseUnit? Unit { get; set; }

    public decimal RequestedQuantity { get; set; }
    public decimal? ProposedQuantity { get; set; }
    public string? ModificationNotes { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal DiscrepancyQuantity { get; set; }
}
