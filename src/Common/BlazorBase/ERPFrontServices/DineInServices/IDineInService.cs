namespace BlazorBase.ERPFrontServices.DineInServices;

public interface IDineInService
{
    public Task<ICollection<TableGroupToReturnDto>> GetTableGroupsAsync();

    public Task<ICollection<TableToReturnDto>> GetTablesByGroupId(int tableGroupId);
    public Task<ICollection<TableToReturnDto>> GetTables();
    public Task<ICollection<UserToReturnDto>> GetCaptainOrders();

    // CRUD operations for Table Groups
    public Task<ServiceResponse<TableGroupToReturnDto>> CreateTableGroup(TableGroupDto tableGroup);
    public Task<ServiceResponse<TableGroupToReturnDto>> UpdateTableGroup(int id, TableGroupToReturnDto tableGroup);
    public Task<ServiceResponse<bool>> DeleteTableGroup(int id);

    // CRUD operations for Tables
    public Task<ServiceResponse<TableToReturnDto>> CreateTable(TableDto table);
    public Task<ServiceResponse<TableToReturnDto>> UpdateTable(int id, TableToReturnDto table);
    public Task<ServiceResponse<bool>> DeleteTable(int id);
}
