namespace POS.Core.Services.Contract.DineInServices;

public interface IDineInService
{
    public Task<bool> CreateTableGroupAsync(TableGroup tableGroup);
    public Task<IEnumerable<TableGroup>> GetAllTableGroupsAsync();
    public Task<TableGroup?> GetTableGroupByIdAsync(int id);
    public Task<bool> DeleteTableGroupAsync(int id);
    public Task<bool> UpdateTableGroupAsync(int id, TableGroup tableGroup);


    public Task<bool> CreateTableAsync(Table table);
    public Task<IEnumerable<Table>> GetAllTablesAsync();
    public Task<Table?> GetTableByIdAsync(int id);
    public Task<bool> UpdateTableAsync(int id, Table table);
    public Task<bool> DeleteTableAsync(int id);
    
    public Task<IEnumerable<Table>> GetTablesByGroupId(int groupId);

}