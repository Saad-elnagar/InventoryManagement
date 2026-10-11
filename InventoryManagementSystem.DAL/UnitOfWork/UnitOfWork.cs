using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;
using InventoryManagementSystem.DAL.Repository.Implementation;

namespace InventoryManagementSystem.DAL;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenaricRepository<T> GenaricRepository<T>()
        where T : BaseEntity
    {
        if (_repositories.TryGetValue(typeof(T), out var repository))
        {
            return (IGenaricRepository<T>)repository;
        }

        var newRepository = new GenaricRepository<T>(_context);

        _repositories.Add(typeof(T), newRepository);

        return newRepository;
    }

    public async Task <int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}