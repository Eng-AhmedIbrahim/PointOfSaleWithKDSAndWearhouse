namespace BlazorBase.API;

public record ApiEndpoints
{
    public string? GetAllCategories { get; set; }
    public string? GetItemsByCategoryId { get; set; }
    public string? GetAllRoles { get; set; }
    public string? GetUserPermissions { get; set; }
    public string? GetOrderSettings { get; set; }
    public string? GetTableGroups { get; set; }
    public string? GetTablesByGroupId { get; set; }
    public string? GetUsersByRole { get; set; }
    public string? GetAppDate { get; set; }
    public string? UpdateAppDate { get; set; }
    public string? UpdateOrderNumber { get; set; }
    public string? UpdateTables { get; set; }
    public string? CloseDay { get; set; }
    public string? GetAllDeliveryCustomerTitles { get; set; }
    public string? GetBranches { get; set; }
    public string? GetZoneByBranchId { get; set; }
    public string? GetCustomerByPhone { get; set; }
    public string? GetAllDeliveryCustomers { get; set; }
    public string? CreateNewCustomer { get; set; }
    public string? AddNewCustomerAddress { get; set; }
    public string? CreateOrder { get; set; }
    
    // DineIn Order endpoints
    public string? CreateDineInOrder { get; set; }
    public string? UpdateDineInOrder { get; set; }
    public string? GetDineInOrderByTableId { get; set; }
    public string? GetOpenOrdersByTableId { get; set; }
    public string? GetAllOpenDineInOrders { get; set; }
    public string? CloseDineInOrder { get; set; }
    public string? VoidDineInOrder { get; set; }
    public string? AddItemsToDineInOrder { get; set; }
    public string? UpdateDineInOrderDiscount { get; set; }
    public string? TransferDineInOrder { get; set; }
    public string? MergeDineInOrders { get; set; }
    public string? SplitDineInOrder { get; set; }
    public string? VoidDineInItems { get; set; }

    // Table Management endpoints
    public string? CreateTable { get; set; }
    public string? UpdateTable { get; set; }
    public string? DeleteTable { get; set; }
    public string? CreateTableGroup { get; set; }
    public string? UpdateTableGroup { get; set; }
    public string? DeleteTableGroup { get; set; }
    public string? GetAllTables { get; set; }
    
    // Order Track endpoints
    public string? TrackOrderAction { get; set; }
    public string? GetOrderTrackingHistory { get; set; }
    public string? GetOrderTrackingByDateRange { get; set; }

    public string? GetUnCompletedDeliveryOrders { get; set; }
    public string? DispatchOrder { get; set; }
    public string? CollectDelivery { get; set; }
    public string? UnDispatchOrder { get; set; }
    public string? CollectDriverOrders { get; set; }
    public string? CollectAllOrders { get; set; }
    public string? GetAllZones { get; set; }
    public string? CreateZone { get; set; }
    public string? UpdateZone { get; set; }
    public string? DeleteZone { get; set; }

    // Payment Method endpoints
    public string? GetPaymentMethods { get; set; }
    public string? CreatePaymentMethod { get; set; }
    public string? UpdatePaymentMethod { get; set; }
    public string? DeletePaymentMethod { get; set; }
    public string? GetDispatcherSettings { get; set; }
    public string? UpdateDispatcherSettings { get; set; }

    // Company endpoints
    public string? CreateCompany { get; set; }
    public string? GetAllCompanies { get; set; }
    public string? GetCompanyById { get; set; }
    public string? UpdateCompany { get; set; }
    public string? DeleteCompany { get; set; }

    // Branch endpoints
    public string? CreateBranch { get; set; }
    public string? GetAllBranches { get; set; }
    public string? GetBranchById { get; set; }
    public string? UpdateBranch { get; set; }
    public string? DeleteBranch { get; set; }

    // Complaint endpoints
    public string? CreateComplaint { get; set; }
    public string? GetAllComplaints { get; set; }
    public string? GetComplaintById { get; set; }
    public string? UpdateComplaintStatus { get; set; }
    public string? GetDriverSettlement { get; set; }

    // Kitchen & Printer endpoints
    public string? GetAllKitchenTypes { get; set; }
    public string? GetAllKitchenPrinters { get; set; }
    public string? GetInstalledPrinters { get; set; }

    // User & Account endpoints
    public string? CreateUser { get; set; }
    public string? GetUsers { get; set; }
    public string? GetUsersWithRoles { get; set; }
    public string? DeleteUser { get; set; }
    public string? UpdateUser { get; set; }
    public string? CreateRole { get; set; }
    public string? DeleteRole { get; set; }
    public string? CheckUserExists { get; set; }
    public string? GetAllPermissions { get; set; }
    public string? GetRolePermissions { get; set; }
    public string? UpdateRolePermissions { get; set; }
    public string? AdminResetPassword { get; set; }
    public string? ChangeOwnPassword { get; set; }
    public string? GetPosFeatureSettings { get; set; }
    public string? UpdatePosFeatureSettings { get; set; }
    public string? GetHqSettings { get; set; }
    public string? UpdateHqSettings { get; set; }
    public string? TestHqConnection { get; set; }
    public string? SyncHqData { get; set; }
}