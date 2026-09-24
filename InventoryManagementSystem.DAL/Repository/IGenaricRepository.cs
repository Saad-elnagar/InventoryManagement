using System.Linq.Expressions;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.DAL.Repository;

public interface IGenaricRepository<T> where T : BaseEntitiy
{
   Task<T> GetByIdAsync(int id);
   Task<IEnumerable<T>> GetAllAsync();
   Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
   Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
   
   // Pagination
   Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
      int pageNumber, 
      int pageSize, 
      Expression<Func<T, bool>> predicate = null);
   // Add Operations
   Task<T> AddAsync(T entity);
   Task AddRangeAsync(IEnumerable<T> entities);
        
   // Update Operations
   Task UpdateAsync(T entity);
   Task UpdateRangeAsync(IEnumerable<T> entities);
        
   // Delete Operations
   Task DeleteAsync(T entity);
   Task DeleteRangeAsync(IEnumerable<T> entities);
   Task DeleteByIdAsync(int id);
        
   // Count
   Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
        
   // Exists
   Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
   
    
    
}