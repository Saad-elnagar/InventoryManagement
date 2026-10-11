using System.Linq.Expressions;
using InventoryManagementSystem.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.DAL.Repository.Implementation;

public class GenaricRepository<T> : IGenaricRepository<T>
    where T : BaseEntity
{
    private readonly ApplicationDbContext _context;

    public GenaricRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context
            .Set<T>()
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        var item = await _context
            .Set<T>()
            .FindAsync(id);

        return item;
    }

    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate)
    {
        return await _context
            .Set<T>()
            .AnyAsync(predicate);
    }

    public async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null)
    {
        var query = _context.Set<T>().AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync();
    }

    public async Task<decimal> SumAsync(
        Expression<Func<T, decimal>> selector)
    {
        return await _context
            .Set<T>()
            .AsNoTracking()
            .SumAsync(selector);
    }
    public async Task<IEnumerable<T>> GetWhereAsync(
        Expression<Func<T, bool>> predicate)
    {
        return await _context
            .Set<T>()
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync();
    }

    public Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null)
    {
        return GetPagedAsync(
            page,
            pageSize,
            predicate,
            entity => entity.Id);
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync<TKey>(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TKey>> orderBy)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context
            .Set<T>()
            .AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(orderBy)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(T entity)
    {
        await _context
            .Set<T>()
            .AddAsync(entity);
    }

    public void Update(T entity)
    {
        _context
            .Set<T>()
            .Update(entity);

        
    }

    public void Delete(T entity)
    {
        _context
            .Set<T>()
            .Remove(entity);

       
    }
}