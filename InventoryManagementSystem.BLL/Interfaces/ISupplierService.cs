using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDTO>> GetAllAsync();

    Task<SupplierDTO?> GetByIdAsync(int id);

    Task<SupplierDTO> CreateAsync(SupplierDTO dto);

    Task<bool> UpdateAsync(int id, SupplierDTO dto);

    Task<bool> DeleteAsync(int id);
}