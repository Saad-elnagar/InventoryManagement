using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Service;

public class StockMovementService : IStockMovementService
{
    private readonly IUnitOfWork _unitOfWork;

    public StockMovementService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<StockMovementDTO>> GetAllAsync()
    {
        try
        {
            var movements =
                await _unitOfWork.GenaricRepository<StockMovement>()
                    .GetAllAsync();

            var productIds = movements
                .Select(m => m.ProductId)
                .Distinct()
                .ToList();

            var products =
                await _unitOfWork.GenaricRepository<Product>()
                    .GetWhereAsync(p => productIds.Contains(p.Id));

            var productDictionary = products
                .ToDictionary(p => p.Id, p => p.Name);

            return movements.Select(m => new StockMovementDTO
            {
                Id = m.Id,
                ProductId = m.ProductId,
                ProductName = productDictionary.TryGetValue(
                    m.ProductId,
                    out var productName)
                    ? productName
                    : null,
                Quantity = m.Quantity,
                MovementType = Enum.Parse<StockMovementType>(
                    m.MovementType),
                MovementDate = m.MovementDate,
                ReferenceType = m.ReferenceType,
                ReferenceId = m.ReferenceId,
                Reason = m.Reason,
                Notes = m.Notes
            });
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting stock movements.",
                ex);
        }
    }

    public async Task<StockMovementDTO?> GetByIdAsync(int id)
    {
        try
        {
            var movement =
                await _unitOfWork.GenaricRepository<StockMovement>()
                    .GetByIdAsync(id);

            if (movement == null)
                return null;

            var product =
                await _unitOfWork.GenaricRepository<Product>()
                    .GetByIdAsync(movement.ProductId);

            return new StockMovementDTO
            {
                Id = movement.Id,
                ProductId = movement.ProductId,
                ProductName = product?.Name!,
                Quantity = movement.Quantity,
                MovementType = Enum.Parse<StockMovementType>(
                    movement.MovementType),
                MovementDate = movement.MovementDate,
                ReferenceType = movement.ReferenceType,
                ReferenceId = movement.ReferenceId,
                Reason = movement.Reason,
                Notes = movement.Notes
            };
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting stock movement.",
                ex);
        }
    }

    public async Task<IEnumerable<StockMovementDTO>> GetByProductIdAsync(
        int productId)
    {
        try
        {
            var productExists =
                await _unitOfWork.GenaricRepository<Product>()
                    .AnyAsync(p => p.Id == productId);

            if (!productExists)
                throw new Exception("Product not found.");

            var movements =
                await _unitOfWork.GenaricRepository<StockMovement>()
                    .GetWhereAsync(m => m.ProductId == productId);

            var product =
                await _unitOfWork.GenaricRepository<Product>()
                    .GetByIdAsync(productId);

            return movements.Select(m => new StockMovementDTO
            {
                Id = m.Id,
                ProductId = m.ProductId,
                ProductName = product?.Name!,
                Quantity = m.Quantity,
                MovementType = Enum.Parse<StockMovementType>(
                    m.MovementType),
                MovementDate = m.MovementDate,
                ReferenceType = m.ReferenceType,
                ReferenceId = m.ReferenceId,
                Reason = m.Reason,
                Notes = m.Notes
            });
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting product stock movements.",
                ex);
        }
    }

    public async Task<IEnumerable<StockMovementDTO>> GetByTypeAsync(
        StockMovementType movementType)
    {
        try
        {
            var movements =
                await _unitOfWork.GenaricRepository<StockMovement>()
                    .GetWhereAsync(m =>
                        m.MovementType == movementType.ToString());

            var productIds = movements
                .Select(m => m.ProductId)
                .Distinct()
                .ToList();

            var products =
                await _unitOfWork.GenaricRepository<Product>()
                    .GetWhereAsync(p => productIds.Contains(p.Id));

            var productDictionary = products
                .ToDictionary(p => p.Id, p => p.Name);

            return movements.Select(m => new StockMovementDTO
            {
                Id = m.Id,
                ProductId = m.ProductId,
                ProductName = productDictionary.TryGetValue(
                    m.ProductId,
                    out var productName)
                    ? productName
                    : null,
                Quantity = m.Quantity,
                MovementType = Enum.Parse<StockMovementType>(
                    m.MovementType),
                MovementDate = m.MovementDate,
                ReferenceType = m.ReferenceType,
                ReferenceId = m.ReferenceId,
                Reason = m.Reason,
                Notes = m.Notes
            });
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting stock movements by type.",
                ex);
        }
    }
}