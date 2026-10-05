using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;
using InventoryManagementSystem.DAL.Repository.Implementation;
using Microsoft.EntityFrameworkCore.Storage;

namespace InventoryManagementSystem.DAL;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    private readonly Dictionary<Type, object> _repositories = new();

    private IDbContextTransaction? _transaction;

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

        var newRepository =
            new GenaricRepository<T>(_context);

        _repositories.Add(
            typeof(T),
            newRepository);

        return newRepository;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
            return;

        _transaction =
            await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
            return;

        await _transaction.CommitAsync();

        await _transaction.DisposeAsync();

        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
            return;

        await _transaction.RollbackAsync();

        await _transaction.DisposeAsync();

        _transaction = null;
    }
}