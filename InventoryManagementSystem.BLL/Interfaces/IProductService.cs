using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDTO>> GetAllAsync();
    Task<ProductDTO?> GetByIdAsync(int id);
    Task<ProductDTO> CreateAsync(ProductDTO dto);
    Task<bool> UpdateAsync(int id, ProductDTO dto);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<ProductDTO>> GetLowStockAsync();
}