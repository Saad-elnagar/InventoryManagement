using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Pagination;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDTO>> GetAllAsync();

    Task<PaginationResult<CategoryDTO>> SearchAsync(string? search, int page = 1, int pageSize = 10);

    Task<CategoryDTO?> GetByIdAsync(int id);

    Task<CategoryDTO> CreateAsync(CategoryDTO dto);

    Task<bool> UpdateAsync(int id, CategoryDTO dto);

    Task<bool> DeleteAsync(int id);
}