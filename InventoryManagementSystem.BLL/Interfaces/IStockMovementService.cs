using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IStockMovementService
{
    Task<IEnumerable<StockMovementDTO>> GetAllAsync(
        int? productId = null,
        string? movementType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    Task<StockMovementDTO?> GetByIdAsync(int id);

    Task<StockMovementDTO> CreateAsync(
        CreateStockMovementDTO dto);
}