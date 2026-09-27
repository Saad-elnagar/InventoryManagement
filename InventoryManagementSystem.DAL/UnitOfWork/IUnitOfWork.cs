using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

namespace InventoryManagementSystem.DAL;

public interface IUnitOfWork
{
    public IGenaricRepository<T> GenaricRepository<T>() where T : BaseEntity;
    public int SaveChangesAsync();

}