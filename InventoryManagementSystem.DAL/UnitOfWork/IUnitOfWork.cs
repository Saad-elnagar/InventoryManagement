using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

public interface IUnitOfWork
{
    IGenaricRepository<T> GenaricRepository<T>()
        where T : BaseEntity;

    Task<int> SaveChangesAsync();

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}