namespace POS.Contract.Dtos.InventoryDtos;

public class BranchRequisitionVm
{
    public int Id { get; set; }
    public string RequisitionNumber { get; set; } = string.Empty;
    public int RequestingBranchId { get; set; }
    public string? BranchName { get; set; }
    public int TargetWarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public WarehouseLookupVm? TargetWarehouse { get; set; }
    public int Status { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public int LineCount { get; set; }
    public List<BranchRequisitionLineVm> Lines { get; set; } = new();
}

public class WarehouseLookupVm
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
}

public class BranchRequisitionLineVm
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public WarehouseProductLookupVm? Product { get; set; }
    public int UnitId { get; set; }
    public WarehouseUnitLookupVm? Unit { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal? ProposedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal DiscrepancyQuantity { get; set; }
}

public class WarehouseProductLookupVm
{
    public int Id { get; set; }
    public string? SKU { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
}

public class WarehouseUnitLookupVm
{
    public int Id { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
}