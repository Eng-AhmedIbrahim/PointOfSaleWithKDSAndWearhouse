using POS.Core.Specifications.DineInSpecs;

namespace POS.Services.DineInService;

public class DineInService : IDineInService
{
    private readonly IUnitOfWork _unitOfWork;

    public DineInService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    public async Task<bool> CreateTableAsync(Table table)
    {
        try
        {
            if (table is null) return false;

            table.BranchID = await GetBranchId();
            await _unitOfWork.Repository<Table>().AddAsync(table);

            return await _unitOfWork.CompleteAsync() > 0;
        }
        catch (Exception ex)
        {
            Log.Error($"Cant Create Table: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateTableGroupAsync(TableGroup tableGroup)
    {
        try
        {
            if (tableGroup is null) return false;

            var tables = await _unitOfWork.Repository<TableGroup>().GetAllAsync();

            tableGroup.Id = tables.LastOrDefault()!.Id + 1;
            tableGroup.CreationDate = DateTime.Now;
            tableGroup.BranchID = await GetBranchId();
            await _unitOfWork.Repository<TableGroup>().AddAsync(tableGroup);

            return await _unitOfWork.CompleteAsync() > 0;
        }
        catch (Exception ex)
        {
            Log.Error($"Cant Create Table Group: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteTableAsync(int id)
    {
        try
        {
            var table = await _unitOfWork.Repository<Table>().GetByIdAsync(id);
            if (table == null) return false;
            _unitOfWork.Repository<Table>().Delete(table);
            return await _unitOfWork.CompleteAsync() > 0;
        }
        catch (Exception ex)
        {
            Log.Error($"Cant Delete Table: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteTableGroupAsync(int id)
    {
        try
        {
            var tableGroup = await _unitOfWork.Repository<TableGroup>().GetByIdAsync(id);
            if (tableGroup == null) return false;
            _unitOfWork.Repository<TableGroup>().Delete(tableGroup);
            return await _unitOfWork.CompleteAsync() > 0;
        }
        catch (Exception ex)
        {
            Log.Error($"Cant Delete Table Group: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<TableGroup>> GetAllTableGroupsAsync()
    => await _unitOfWork.Repository<TableGroup>().GetAllAsync();

    public async Task<IEnumerable<Table>> GetAllTablesAsync()
     => await _unitOfWork.Repository<Table>().GetAllAsync();

    public async Task<Table?> GetTableByIdAsync(int id)
    => await _unitOfWork.Repository<Table>().GetByIdAsync(id);

    public async Task<TableGroup?> GetTableGroupByIdAsync(int id)
    => await _unitOfWork.Repository<TableGroup>().GetByIdAsync(id);

    public async Task<IEnumerable<Table>> GetTablesByGroupId(int groupId)
    {
        TableSpecs tableSpecs = new TableSpecs(groupId);
        var tables =  await _unitOfWork.Repository<Table>().GetAllWithSpecificationAsync(tableSpecs);
        return tables;
    }

    public async Task<bool> UpdateTableAsync(int id, Table table)
    {
        if (table == null) return false;
        
        var oldTable= await GetTableByIdAsync(id);

        oldTable!.TableName = table.TableName;
        oldTable.GroupID = table.GroupID;
        oldTable.SeatsCount = oldTable.SeatsCount;
        oldTable.BranchID = oldTable.BranchID;
        
        _unitOfWork.Repository<Table>().Update(oldTable);

        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool> UpdateTableGroupAsync(int id, TableGroup tableGroup)
    {
        if (tableGroup == null) return false;

        var oldTableGroup = await GetTableGroupByIdAsync(id);

        oldTableGroup!.GroupName = tableGroup.GroupName;
        oldTableGroup.BranchID = oldTableGroup.BranchID;

        _unitOfWork.Repository<TableGroup>().Update(oldTableGroup);

        return await _unitOfWork.CompleteAsync() > 0;
    }


    private async Task<int> GetBranchId()
    {
        var branch = await _unitOfWork.Repository<Branch>().GetAllAsync();
        return branch.First().Id;
    }
}