using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Pos.Repository.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _dbContext;

    public GenericRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(T entity)
        => await _dbContext.Set<T>().AddAsync(entity);

    public async Task<IReadOnlyList<T>> GetAllAsync()
        => await _dbContext.Set<T>().AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<T>> GetAllWithSpecificationAsync(ISpecifications<T> spec)
        => await ApplySpecification(spec).ToListAsync();

    public async Task<T?> GetByIdAsync(int id)
        => await _dbContext.Set<T>().FindAsync(id);

    public async Task<T?> GetByIdWithSpecificationAsync(ISpecifications<T> specs)
        => await ApplySpecification(specs).FirstOrDefaultAsync();

    public async Task<T?> GetByIdWithSpecificationTrackedAsync(ISpecifications<T> specs)
        => await ApplySpecificationTracked(specs).FirstOrDefaultAsync();

    public async Task<int> GetCountAsync(ISpecifications<T> spec)
        => await ApplySpecification(spec).CountAsync();

    public void Delete(T entity)
    {
        if (_dbContext.Entry(entity).State == EntityState.Detached)
            _dbContext.Set<T>().Attach(entity);

        _dbContext.Set<T>().Remove(entity);
    }

    public void Update(T entity)
    {
        var entry = _dbContext.Entry(entity);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Modified;
            return;
        }

        var local = _dbContext.Set<T>()
            .Local
            .FirstOrDefault(e => e.Id.Equals(entity.Id));

        if (local is not null)
        {
            _dbContext.Entry(local).State = EntityState.Detached;
        }

        _dbContext.Update(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
        => await _dbContext.Set<T>().AddRangeAsync(entities);

    public void UpdateRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            var local = _dbContext.Set<T>()
                .Local
                .FirstOrDefault(entry => entry.Id.Equals(entity.Id));

            if (local is not null)
                _dbContext.Entry(local).State = EntityState.Detached;

            _dbContext.Entry(entity).State = EntityState.Modified;
        }
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        => await _dbContext.Set<T>().AnyAsync(predicate);

    IQueryable<T> ApplySpecification(ISpecifications<T> spec)
        => SpecificationEvaluator<T>.GetQuery(_dbContext.Set<T>().AsNoTrackingWithIdentityResolution(), spec);

    IQueryable<T> ApplySpecificationTracked(ISpecifications<T> spec)
        => SpecificationEvaluator<T>.GetQuery(_dbContext.Set<T>(), spec);

    public async Task<T?> GetUserSettingByIdAsync(string id)
    => await _dbContext.Set<T>(id).FirstOrDefaultAsync();

    //public async Task UpdateExecutionAsync(T entity, params Expression<Func<T, object>>[] properties)
    //{
    //    var dbSet = _dbContext.Set<T>();

    //    // Get primary key name and value
    //    var entityType = _dbContext.Model.FindEntityType(typeof(T))!;
    //    var keyProperty = entityType.FindPrimaryKey()!.Properties.First();
    //    var keyName = keyProperty.Name;
    //    var keyValue = typeof(T).GetProperty(keyName)!.GetValue(entity);

    //    var query = dbSet.Where(e => EF.Property<object>(e, keyName)!.Equals(keyValue!));

       
    //    await query.ExecuteUpdateAsync(setter =>
    //    {
    //        foreach(var propertys in properties)
    //        {
    //            var member = GetMemberExpression(propertys.Body);
    //            var propertyName = member.Member.Name;
    //            var property = typeof(T).GetProperty(propertyName);
    //            var value = property!.GetValue(entity);
    //            var parameter = Expression.Parameter(typeof(T), "e");
    //            var propertyAccess = Expression.Property(parameter, propertyName);
    //            var lambda = Expression.Lambda(propertyAccess, parameter);

    //            var method = typeof(SettersExtensions)
    //                .GetMethods()
    //                .First(m => m.Name == "SetProperty" && m.GetParameters().Length == 3)
    //                .MakeGenericMethod(typeof(T), property.PropertyType);


    //        }
    //    })
    //}


    private MemberExpression GetMemberExpression(Expression body)
    {
        if (body is UnaryExpression unary && unary.Operand is MemberExpression member)
            return member;

        if (body is MemberExpression memberExpr)
            return memberExpr;

        throw new InvalidOperationException("Invalid expression format.");
    }


    private object GetValueLambda(Type type, object? value)
    {
        var parameter = Expression.Parameter(type, "_");
        var constant = Expression.Constant(value, type);
        var lambdaType = typeof(Func<,>).MakeGenericType(type, type);
        return Expression.Lambda(lambdaType, constant, parameter).Compile();
    }

}
