namespace POS.API.Controllers;

public class BranchController : BaseApiController
{
    private readonly IBranchService _branchService;
    private readonly IMapper _mapper;

    public BranchController(IBranchService branchService ,IMapper mapper)
    {
        _branchService = branchService;
        _mapper = mapper;
    }

    [ProducesResponseType(typeof(BranchToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<IActionResult> CreateBranch([FromBody] BranchDto branchDto)
    {
        if (branchDto is null)
            return BadRequest(new ApiResponse(400));

        var mappedBranch = _mapper.Map<BranchDto, Branch>(branchDto);

        var branch = await _branchService.CrateBranchAsync(mappedBranch);

        if (branch is null)
            return BadRequest(new ApiResponse(400));

         var branchToReturn= _mapper.Map<Branch, BranchToReturnDto>(branch);

        return Ok(branchToReturn);
    }

    [ProducesResponseType(typeof(IReadOnlyList<BranchToReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("GetAllBranches")]
    public async Task<IActionResult> GetAllBranches()
    {
        var branches = await _branchService.GetBranchesAsync();
        var mappedBranchs = _mapper.Map<IReadOnlyList<Branch>, IReadOnlyList<BranchToReturnDto>>(branches);

        if (mappedBranchs is null)
            return NotFound(new ApiResponse(404));

        return Ok(mappedBranchs);
    }


    [ProducesResponseType(typeof(BranchToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpGet("{branchId}")]
    public async Task<IActionResult> GetBranchById(int branchId)
    {
        var branch = await _branchService!.GetBranchByIdAsync(branchId)!;

        var mappedBranch = _mapper.Map<Branch,BranchToReturnDto>(branch??new());   
        if (mappedBranch is null)
            return NotFound(new ApiResponse(404));

        return Ok(mappedBranch);
    }


    [ProducesResponseType(typeof(Branch), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpPut]
    public async Task<IActionResult> UpdateBranch([FromBody] UpdatedBranchDto branch)
    {
        var storedBranch = await _branchService.GetBranchByIdAsync(branch.BranchId)!;
     
        var newBranch = _mapper.Map<UpdatedBranchDto, Branch>(branch);
        newBranch.Id = storedBranch!.Id;

        if (storedBranch is null)
            return NotFound(new ApiResponse(404));

        var result = await _branchService.UpdateBranchAsync(storedBranch, newBranch);

        if (result is null)
            return NotFound(new ApiResponse(404));

        return Ok(result);
    }


    [ProducesResponseType(typeof(BranchToReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [HttpDelete]
    public async Task<IActionResult> DeleteCompany(int branchId)
    {
        var branch = await _branchService.GetBranchByIdAsync(branchId)!;
        if (branch is null)
            return NotFound(new ApiResponse(404));

        var result = await _branchService.DeleteBranch(branch);
        if (result is false)
            return NotFound(new ApiResponse(404));

        return Ok("BranchDeletedSuccessfully");
    }

}
