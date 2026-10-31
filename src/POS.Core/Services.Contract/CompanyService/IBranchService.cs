namespace POS.Core.Services.Contract.CompanyService;

public interface IBranchService
{
    public Task<Branch?> CrateBranchAsync(Branch Branch);
    public Task<Branch?> UpdateBranchAsync(Branch oldBranch, Branch newBranch);
    public Task<IReadOnlyList<Branch>?> GetBranchesAsync();
    public Task<Branch?>? GetBranchByIdAsync(int BranchId);
    public Task<bool> DeleteBranch(Branch Branch);
}