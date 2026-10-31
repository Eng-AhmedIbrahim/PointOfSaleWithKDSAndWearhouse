using Pos.Repository.Repositories;

namespace POS.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;
    private readonly Hashtable _repositories;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _repositories = new Hashtable();
    }

    public IGenericRepository<T> Repository<T>() where T : BaseEntity
    {
        var key = typeof(T).FullName;

        if (!_repositories.ContainsKey(key!))
        {
            var repositoryInstance = new GenericRepository<T>(_dbContext);
            _repositories.Add(key!, repositoryInstance);
        }

        return (IGenericRepository<T>)_repositories[key!]!;
    }

    public async Task<int> CompleteAsync()
        => await _dbContext.SaveChangesAsync();

    public async ValueTask DisposeAsync()
        => await _dbContext.DisposeAsync();

    public IDbContextTransaction BeginTransaction()
    {
        return _dbContext.Database.BeginTransaction();
    }

    public IExecutionStrategy CreateExecutionStrategy()
    {
        return _dbContext.Database.CreateExecutionStrategy();
    }
}