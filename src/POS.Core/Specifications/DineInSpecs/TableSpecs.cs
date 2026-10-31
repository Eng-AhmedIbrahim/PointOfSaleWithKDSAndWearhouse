namespace POS.Core.Specifications.DineInSpecs;

public class TableSpecs : BaseSpecifications<Table>
{
    public TableSpecs(int groupId) : base(x => x.GroupID == groupId) { }
}