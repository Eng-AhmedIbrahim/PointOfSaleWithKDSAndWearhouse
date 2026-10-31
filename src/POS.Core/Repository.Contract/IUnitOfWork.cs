using Microsoft.EntityFrameworkCore.Storage;

namespace POS.Core.Repository.Contract;

public interface IUnitOfWork:IAsyncDisposable
{
    IGenericRepository<T> Repository<T>() where T :BaseEntity;
    Task<int> CompleteAsync();
    public IDbContextTransaction BeginTransaction();
    IExecutionStrategy CreateExecutionStrategy();
}