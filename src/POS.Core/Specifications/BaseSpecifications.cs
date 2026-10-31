namespace POS.Core.Specifications;

public class BaseSpecifications<T> : ISpecifications<T> where T : BaseEntity
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    public List<Expression<Func<T, object>>> Includes { get; protected set; } = [];
    public List<string> IncludeStrings { get; protected set; } = [];
    public List<string> ThenIncludes { get; protected set; } = [];

    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

    public int Skip { get; protected set; }
    public int Take { get; protected set; }
    public bool IsPaginationEnabled { get; protected set; }
    public bool AsSplitQuery { get; protected set; }

    public BaseSpecifications()
    {
    }

    public BaseSpecifications(Expression<Func<T, bool>> criteria)
        => Criteria = criteria;

    protected void AddOrderBy(Expression<Func<T, object>> orderBy)
        => OrderBy = orderBy;

    protected void AddOrderByDesc(Expression<Func<T, object>> orderByDesc) 
        => OrderByDescending = orderByDesc;

    protected void EnablePagination(int skip, int take)
    {
        IsPaginationEnabled = true;
        Skip = skip;
        Take = take;
    }

    public void AddThenInclude(string thenIncludeString)
    {
        ThenIncludes.Add(thenIncludeString);
    }

    protected void EnableSplitQuery()
    {
        AsSplitQuery = true;
    }
}