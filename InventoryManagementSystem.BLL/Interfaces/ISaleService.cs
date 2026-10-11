using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface ISaleService
{
    Task<IEnumerable<SaleDTO>> GetAllAsync();
    Task<SaleDTO?> GetByIdAsync(int id);
    Task<SaleDTO> CreateAsync(SaleDTO dto);
}