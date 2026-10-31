namespace POS.Core.Specifications.BranchSpecs;

public class BranchSpecs : BaseSpecifications<Branch>
{
    public BranchSpecs(string branchName) : base(b=>b.Name == branchName)
    {
    }
}