using Dsw2026Tpi.Domain.Entities;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Domain.Interfaces;

public interface IPersistence
{
    Task<T?> GetById<T>(Guid id, params string[] include) where T : EntityBase;
    Task<IEnumerable<T>?> GetAll<T>(params string[] include) where T : EntityBase;
    Task<T?> First<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase;

    //Igual que First, pero el que llama arma los includes (por ejemplo filtrados: solo disponibilidades no eliminadas)
    //para traer el agregado completo en una sola consulta.
    Task<T?> FirstWith<T>(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>> shape) where T : EntityBase;
    Task<bool> Any<T>(Expression<Func<T, bool>> predicate) where T : EntityBase;
    Task<IEnumerable<T>?> GetFiltered<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase;
    Task<T> Add<T>(T entity) where T : EntityBase;
    Task<T> Update<T>(T entity) where T : EntityBase;
    Task<T> Delete<T>(T entity) where T : EntityBase;
    Task AddRange<T>(IEnumerable<T> entities) where T : EntityBase;
    Task UpdateRange<T>(IEnumerable<T> entities) where T : EntityBase;
    Task<int> SaveChangesAsync();
    Task ExecuteInTransactionAsync(Func<Task> operation);
    Task<Pagination<T>> Paginate<T, TKey>(
        int pageSize, 
        int pageIndex, 
        Expression<Func<T, bool>> predicate, 
        Expression<Func<T, TKey>> sortOrder, 
        params string[] includes) 
        where T : EntityBase;
}
