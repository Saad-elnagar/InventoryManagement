using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

public interface IUnitOfWork
{
    public IGenaricRepository<T> GenaricRepository<T>() where T : BaseEntity;
    public Task<int> SaveChangesAsync();

}