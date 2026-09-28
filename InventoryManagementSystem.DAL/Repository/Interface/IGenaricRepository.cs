using System.Linq.Expressions;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.DAL.Repository;

public interface IGenaricRepository<T>
    where T : BaseEntity
{
    Task<IEnumerable<T>> GetAllAsync();

    Task<T> GetByIdAsync(int id);

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate);
    
    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null);

    Task<decimal> SumAsync(
        Expression<Func<T, decimal>> selector);


   
    Task<IEnumerable<T>> GetWhereAsync(
        Expression<Func<T, bool>> predicate);

    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null);


    Task AddAsync(T entity);

     void Update(T entity);

    void Delete(T entity);
}