namespace BlazorBase.ERPFrontServices.BranchServices;

public interface IBranchService
{
    Task<IReadOnlyList<BranchToReturnDto>> GetBranches();
    Task<BranchToReturnDto?> GetBranchById(int id);
    Task<BranchToReturnDto?> CreateBranch(BranchDto branchDto);
    Task<BranchToReturnDto?> UpdateBranch(UpdatedBranchDto branchDto);
    Task<bool> DeleteBranch(int id);
}