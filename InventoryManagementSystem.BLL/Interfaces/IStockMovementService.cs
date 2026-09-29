using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IStockMovementService
{
    Task<IEnumerable<StockMovementDTO>> GetAllAsync();

    Task<IEnumerable<StockMovementDTO>> GetByProductIdAsync(
        int productId);

    Task<IEnumerable<StockMovementDTO>> GetByTypeAsync(
        string movementType);

    Task<StockMovementDTO?> GetByIdAsync(int id);
}