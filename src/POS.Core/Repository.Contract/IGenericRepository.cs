namespace POS.Core.Repository.Contract;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> GetAllWithSpecificationAsync(ISpecifications<T>spec);    
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetUserSettingByIdAsync(string id);
    Task<T?> GetByIdWithSpecificationAsync(ISpecifications<T> specs);
    Task<T?> GetByIdWithSpecificationTrackedAsync(ISpecifications<T> specs);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Delete(T entity);
    Task<int> GetCountAsync(ISpecifications<T> spec);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    //public Task UpdateExecutionAsync(T entity, params Expression<Func<T, object>>[] properties);
}