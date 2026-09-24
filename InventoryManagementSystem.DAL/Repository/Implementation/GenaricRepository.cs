using InventoryManagementSystem.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.DAL.Repository.Implementation;

public class GenaricRepository<T> : IGenaricRepository<T> where T : BaseEntity
{
    private readonly ApplicationDbContext _context;

    public GenaricRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<T> GetByIdAsync(int id)
    {
        var items = await _context.Set<T>().FindAsync(id);
        return items!;
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
        _context.SaveChangesAsync();
    }

    public void Delete(T entity)
    {
        _context.Set<T>().Remove(entity);
        _context.SaveChangesAsync();
    }
}