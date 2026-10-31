namespace POS.API.Controllers;

public class DineInController : BaseApiController
{
    private readonly IDineInService _dineInService;
    private readonly IMapper _mapper;

    public DineInController(IDineInService dineInService, IMapper mapper)
    {
        _dineInService = dineInService;
        _mapper = mapper;
    }

    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TableToReturnDto), StatusCodes.Status200OK)]
    [HttpPost("create-table")]
    public async Task<IActionResult> CreateTable([FromBody] TableDto table)
    {
        if (table == null) return BadRequest(new ApiResponse(400, "Table data is required."));

        var mappedTable = _mapper.Map<TableDto, Table>(table);
        var result = await _dineInService.CreateTableAsync(mappedTable);
        var tableToReturn = _mapper.Map<Table, TableToReturnDto>(mappedTable);
        return result ? Ok(tableToReturn) : BadRequest(new ApiResponse(500, "Failed to create table."));
    }

    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TableGroupToReturnDto), StatusCodes.Status200OK)]
    [HttpPost("create-TableGroup")]
    public async Task<IActionResult> CreateTableGroup([FromBody] TableGroupDto tableGroup)
    {
        if (tableGroup == null) return BadRequest(new ApiResponse(400, "Table group data is required."));

        var mappedTableGroup = _mapper.Map<TableGroupDto, TableGroup>(tableGroup);
        
        var result = await _dineInService.CreateTableGroupAsync(mappedTableGroup);

        var mappedTableGroupToReturn = _mapper.Map<TableGroup, TableGroupToReturnDto>(mappedTableGroup);

        return result ? Ok(mappedTableGroupToReturn) : StatusCode(500, "Failed to create table group.");
    }

    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<TableToReturnDto>), StatusCodes.Status200OK)]
    [HttpGet("get-all-tables")]
    public async Task<IActionResult> GetAllTables()
    {
        var tables = await _dineInService.GetAllTablesAsync();
        var mappedTables = _mapper.Map<IEnumerable<Table>, IEnumerable<TableToReturnDto>>(tables);
        return Ok(mappedTables);
    }


    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TableToReturnDto), StatusCodes.Status200OK)]
    [HttpGet("table/{id}")]
    public async Task<IActionResult> GetTableById(int id)
    {
        var table = await _dineInService.GetTableByIdAsync(id);
        var mappedTable = _mapper.Map<Table, TableToReturnDto>(table!);
        return table != null ? Ok(mappedTable) : NotFound(new ApiResponse(404, "Table group not found."));
    }


    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IEnumerable<TableGroupToReturnDto>), StatusCodes.Status200OK)]
    [HttpGet("table-groups")]
    public async Task<IActionResult> GetAllTableGroups()
    {
        var tableGroups = await _dineInService.GetAllTableGroupsAsync();
        var mappedTableGroups = _mapper.Map<IEnumerable<TableGroup>, IEnumerable<TableGroupToReturnDto>>(tableGroups);
        return Ok(mappedTableGroups);
    }


    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TableGroupToReturnDto), StatusCodes.Status200OK)]
    [HttpGet("table-group/{id}")]
    public async Task<IActionResult> GetTableGroupById(int id)
    {
        var tableGroup = await _dineInService.GetTableGroupByIdAsync(id);
        var mappedTableGroup = _mapper.Map<TableGroup, TableGroupToReturnDto>(tableGroup!);
        return mappedTableGroup != null ? Ok(mappedTableGroup) : NotFound(new ApiResponse(404, "Table group not found."));
    }

    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TableToReturnDto), StatusCodes.Status200OK)]
    [HttpGet("tables-by-group-id/{groupId}")]
    public async Task<IActionResult> GetTablesByGroupId(int groupId)
    {
        var tables = await _dineInService.GetTablesByGroupId(groupId);
        var mappedTableGroup = _mapper.Map <IEnumerable<Table>, IEnumerable<TableToReturnDto>>(tables!);
        return mappedTableGroup != null ? Ok(mappedTableGroup) : NotFound(new ApiResponse(404, "Table group not found."));
    }



    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TableToReturnDto), StatusCodes.Status200OK)]
    [HttpPut("table/{id}")]
    public async Task<IActionResult> UpdateTable(int id, [FromBody] Table table)
    {
        if (table == null) return BadRequest(new ApiResponse(400, "Table data is required."));
        var result = await _dineInService.UpdateTableAsync(id, table);
        var mappedTable = _mapper.Map<Table, TableToReturnDto>(table);
        return result ? Ok(mappedTable) : StatusCode(500, "Failed to update table group.");
    }

    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TableGroupToReturnDto), StatusCodes.Status200OK)]
    [HttpPut("table-group/{id}")]
    public async Task<IActionResult> UpdateTableGroup(int id, [FromBody] TableGroup tableGroup)
    {
        if (tableGroup == null) return BadRequest(new ApiResponse(400, "Table group data is required."));
        var result = await _dineInService.UpdateTableGroupAsync(id, tableGroup);
        var mappedTableGroup = _mapper.Map<TableGroup, TableGroupToReturnDto>(tableGroup);
        return result ? Ok(mappedTableGroup) : StatusCode(500, "Failed to update table group.");
    }

    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [HttpDelete("table/{id}")]
    public async Task<IActionResult> DeleteTable(int id)
    {
        var result = await _dineInService.DeleteTableAsync(id);
        return result ? Ok("Table deleted successfully.") : StatusCode(500, "Failed to delete table.");
    }


    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [HttpDelete("table-group/{id}")]
    public async Task<IActionResult> DeleteTableGroup(int id)
    {
        var result = await _dineInService.DeleteTableGroupAsync(id);
        return result ? Ok("Table group deleted successfully.") : StatusCode(500, "Failed to delete table group.");
    }
}