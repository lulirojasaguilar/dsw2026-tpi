using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Data;

public class PersistenceEf : IPersistence
{
    private readonly Dsw2026TpiDbContext _context;

    public PersistenceEf(Dsw2026TpiDbContext context)
    {
        _context = context;
    }

    public async Task<T> Add<T>(
        T entity) 
        where T : EntityBase
    {
        await _context.AddAsync(entity);
        return entity;
    }

    public async Task AddRange<T>(
        IEnumerable<T> entities)
        where T : EntityBase
    {
        await _context.Set<T>().AddRangeAsync(entities);
    }
    public Task<T> Update<T>(
        T entity)
        where T : EntityBase
    {
        _context.Update(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateRange<T>(
        IEnumerable<T> entities)
        where T : EntityBase
    {
        _context.Set<T>().UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task<T> Delete<T>(
        T entity)
        where T : EntityBase
    {
        _context.Remove(entity);
        return Task.FromResult(entity);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task ExecuteInTransactionAsync(
        Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await operation();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
    public async Task<T?> First<T>(
        Expression<Func<T, bool>> 
        predicate, params string[] include) 
        where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).FirstOrDefaultAsync(predicate);
    }
    public async Task<T?> FirstWith<T>(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>> shape)
        where T : EntityBase
    {
        return await shape(_context.Set<T>()).FirstOrDefaultAsync(predicate);
    }

    public async Task<bool> Any<T>(
        Expression<Func<T, bool>> predicate)
        where T : EntityBase
    {
        return await _context.Set<T>().AnyAsync(predicate);
    }

    public async Task<IEnumerable<T>?> GetAll<T>(
        params string[] include) 
        where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).ToListAsync();
    }

    public async Task<T?> GetById<T>(
        Guid id, 
        params string[] include) 
        where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<T>?> GetFiltered<T>(
        Expression<Func<T, bool>> predicate, 
        params string[] include) 
        where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).Where(predicate).ToListAsync();
    }

    //Paginado con base 1: pageIndex = 1 es la primera página. La respuesta devuelve el mismo
    //pageIndex que se pidió; si esa página no existe, devuelve la última que tiene datos.

    public async Task<Pagination<T>> Paginate<T, TKey>(
        int pageSize,
        int pageIndex,
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TKey>> sortOrder,
        params string[] includes)
        where T : EntityBase
    {
        pageSize = Math.Abs(pageSize);
        var requestedPage = Math.Max(1, Math.Abs(pageIndex));

        var filtered = Include(_context.Set<T>(), includes)
                 .Where(predicate)
                 .OrderBy(sortOrder);

        var total = await filtered.CountAsync();
        if (total == 0 || pageSize == 0)
        {
            return new Pagination<T>(pageSize, 1, [], total);
        }

        var lastPage = (int)Math.Ceiling(total / (double)pageSize);
        var page = Math.Min(requestedPage, lastPage);

        var data = await filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new Pagination<T>(pageSize, page, data, total);
    }

    private static IQueryable<T> Include<T>(
        IQueryable<T> query, 
        string[] includes) 
        where T : EntityBase
    {
        var includedQuery = query;

        foreach (var include in includes)
        {
            includedQuery = includedQuery.Include(include);
        }
        return includedQuery;
    }
}
