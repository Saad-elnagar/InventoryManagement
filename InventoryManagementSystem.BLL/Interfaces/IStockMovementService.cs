using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IStockMovementService
{
    Task<IEnumerable<StockMovementDTO>> GetAllAsync();
    Task<StockMovementDTO?> GetByIdAsync(int id);
    Task<IEnumerable<StockMovementDTO>> GetByProductIdAsync(int productId);
    Task<IEnumerable<StockMovementDTO>> GetByTypeAsync(StockMovementType movementType);
}