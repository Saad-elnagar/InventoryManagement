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

    public async Task<T> GetByIdAsync(int id)
    {
        var item = await _context
            .Set<T>()
            .FindAsync(id);

        return item!;
    }

    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate)
    {
        return await _context
            .Set<T>()
            .AnyAsync(predicate);
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

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null)
    {
        var query = _context
            .Set<T>()
            .AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.Id)
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

        await _context.SaveChangesAsync();
    }

    public void Update(T entity)
    {
        _context
            .Set<T>()
            .Update(entity);

        _context.SaveChangesAsync();
    }

    public void Delete(T entity)
    {
        _context
            .Set<T>()
            .Remove(entity);

        _context.SaveChangesAsync();
    }
}