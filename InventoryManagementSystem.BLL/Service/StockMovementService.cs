using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;

namespace InventoryManagementSystem.BLL.Service;

public class StockMovementService : IStockMovementService
{
    public async Task<IEnumerable<StockMovementDTO>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<StockMovementDTO>> GetByProductIdAsync(int productId)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<StockMovementDTO>> GetByTypeAsync(string movementType)
    {
        throw new NotImplementedException();
    }

    public async Task<StockMovementDTO?> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }
}