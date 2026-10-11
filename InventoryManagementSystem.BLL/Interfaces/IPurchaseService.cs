using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IPurchaseService
{
    Task<IEnumerable<PurchaseDTO>> GetAllAsync();

    Task<PurchaseDTO?> GetByIdAsync(int id);

    Task<PurchaseDTO> CreateAsync(PurchaseDTO dto);
}