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
    public string RequisitionNumber { get; set; } = string.Empty; // e.g. REQ-2026-0001
    public int RequestingBranchId { get; set; }
    public POS.Core.Entities.Company.Branch? RequestingBranch { get; set; }

    // المخزن المستهدف بالطلب (الافتراضي هو المخزن الرئيسي أو مخزن آخر يختاره الفرع)
    public int TargetWarehouseId { get; set; }
    public Warehouse? TargetWarehouse { get; set; }

    // لو تم تحويل الطلب لمخزن بديل
    public int? ReroutedToWarehouseId { get; set; }
    public Warehouse? ReroutedToWarehouse { get; set; }
    public string? RerouteReason { get; set; }

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequiredDate { get; set; }

    // التواريخ وتتبع الحالات
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByUserId { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string? DispatchedByUserId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? ReceivedByUserId { get; set; }

    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }

    // أسطر الطلبية مع الكميات ومراحل التعديل
    public ICollection<BranchRequisitionLine> Lines { get; set; } = new List<BranchRequisitionLine>();
}

public class BranchRequisitionLine : BaseEntity
{
    public int RequisitionId { get; set; }
    public BranchRequisition? Requisition { get; set; }

    public int ProductId { get; set; }
    public WarehouseProduct? Product { get; set; }

    public int UnitId { get; set; }
    public WarehouseUnit? Unit { get; set; }

    // 1. الكمية التي طلبها الفرع
    public decimal RequestedQuantity { get; set; }

    // 2. الكمية المقترحة من المخزن (في حال وجود نقص أو تعديل)
    public decimal? ProposedQuantity { get; set; }
    public string? ModificationNotes { get; set; }

    // 3. الكمية المعتمدة النهائية بعد موافقة الطرفين
    public decimal ApprovedQuantity { get; set; }

    // 4. الكمية المشحونة بالفعل
    public decimal DispatchedQuantity { get; set; }

    // 5. الكمية المستلمة فعلياً في الفرع
    public decimal ReceivedQuantity { get; set; }

    // 6. كمية العجز أو التلف أثناء النقل
    public decimal DiscrepancyQuantity { get; set; }
}
