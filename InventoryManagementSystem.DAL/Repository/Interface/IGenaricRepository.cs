using System.Linq.Expressions;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.DAL.Repository;

public interface IGenaricRepository <T> where T : BaseEntity
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(int id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task SaveChangesAsync();
    public  Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);


}