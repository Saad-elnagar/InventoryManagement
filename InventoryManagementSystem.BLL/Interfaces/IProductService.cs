using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Pagination;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDTO>> GetAllAsync();
    Task<ProductDTO?> GetByIdAsync(int id);
    Task<ProductDTO> CreateAsync(ProductDTO dto);
    Task<bool> UpdateAsync(int id, ProductDTO dto);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<ProductDTO>> GetLowStockAsync();
    public Task<PaginationResult<ProductDTO>> GetPagedAsync(int page , int pagesize);
}