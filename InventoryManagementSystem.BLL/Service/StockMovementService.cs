using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Service;

public class StockMovementService : IStockMovementService
{
    private readonly IUnitOfWork _unitOfWork;

    public StockMovementService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<StockMovementDTO>> GetAllAsync(
        int? productId = null,
        string? movementType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var movements =
                await _unitOfWork
                    .GenaricRepository<StockMovement>()
                    .GetAllAsync();

            if (productId.HasValue)
            {
                movements =
                    movements.Where(
                        x => x.ProductId == productId.Value);
            }

            if (!string.IsNullOrWhiteSpace(movementType))
            {
                movements =
                    movements.Where(
                        x =>
                            string.Equals(
                                x.MovementType,
                                movementType,
                                StringComparison.OrdinalIgnoreCase));
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;

                movements =
                    movements.Where(
                        x => x.MovementDate >= from);
            }

            if (toDate.HasValue)
            {
                var to =
                    toDate.Value.Date.AddDays(1).AddTicks(-1);

                movements =
                    movements.Where(
                        x => x.MovementDate <= to);
            }

            movements =
                movements
                    .OrderByDescending(
                        x => x.MovementDate)
                    .ThenByDescending(
                        x => x.Id);

            var productIds =
                movements
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToList();

            var products =
                productIds.Count == 0
                    ? Enumerable.Empty<Product>()
                    : await _unitOfWork
                        .GenaricRepository<Product>()
                        .GetWhereAsync(
                            x => productIds.Contains(x.Id));

            var productDictionary =
                products.ToDictionary(
                    x => x.Id,
                    x => x.Name);

            return movements.Select(
                x => new StockMovementDTO
                {
                    Id = x.Id,

                    ProductId = x.ProductId,

                    ProductName =
                        productDictionary.TryGetValue(
                            x.ProductId,
                            out var productName)
                            ? productName
                            : null,

                    Quantity = x.Quantity,

                    MovementType =
                        x.MovementType,

                    MovementDate =
                        x.MovementDate,

                    ReferenceType =
                        x.ReferenceType,

                    ReferenceId =
                        x.ReferenceId,

                    Reason =
                        x.Reason,

                    Notes =
                        x.Notes
                });
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting stock movements.",
                ex);
        }
    }

    public async Task<StockMovementDTO?> GetByIdAsync(
        int id)
    {
        try
        {
            var movement =
                await _unitOfWork
                    .GenaricRepository<StockMovement>()
                    .GetByIdAsync(id);

            if (movement == null)
                return null;

            var product =
                await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetByIdAsync(
                        movement.ProductId);

            return new StockMovementDTO
            {
                Id =
                    movement.Id,

                ProductId =
                    movement.ProductId,

                ProductName =
                    product?.Name,

                Quantity =
                    movement.Quantity,

                MovementType =
                    movement.MovementType,

                MovementDate =
                    movement.MovementDate,

                ReferenceType =
                    movement.ReferenceType,

                ReferenceId =
                    movement.ReferenceId,

                Reason =
                    movement.Reason,

                Notes =
                    movement.Notes
            };
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting stock movement.",
                ex);
        }
    }

    public async Task<StockMovementDTO> CreateAsync(
        CreateStockMovementDTO dto)
    {
        var manualTypes =
            new HashSet<StockMovementType>
            {
                StockMovementType.Damage,
                StockMovementType.Lost,
                StockMovementType.Found,
                StockMovementType.Adjustment,
                StockMovementType.PurchaseReturn,
                StockMovementType.SaleReturn,
                StockMovementType.OpeningStock,
                StockMovementType.VendorGift
            };

        if (!manualTypes.Contains(dto.MovementType))
        {
            throw new Exception(
                "Purchase and Sale movements are created automatically.");
        }

        if (dto.Quantity <= 0)
        {
            throw new Exception(
                "Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new Exception(
                "Reason is required.");
        }

        var product =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetByIdAsync(
                    dto.ProductId);

        if (product == null)
        {
            throw new Exception(
                "Product not found.");
        }

        var decreaseTypes =
            new HashSet<StockMovementType>
            {
                StockMovementType.Damage,
                StockMovementType.Lost,
                StockMovementType.PurchaseReturn
            };

        var increaseTypes =
            new HashSet<StockMovementType>
            {
                StockMovementType.Found,
                StockMovementType.SaleReturn,
                StockMovementType.OpeningStock,
                StockMovementType.VendorGift
            };

        var delta = 0;

        if (decreaseTypes.Contains(
                dto.MovementType))
        {
            delta =
                -dto.Quantity;
        }
        else if (increaseTypes.Contains(
                     dto.MovementType))
        {
            delta =
                dto.Quantity;
        }
        else if (dto.MovementType ==
                 StockMovementType.Adjustment)
        {
            delta =
                dto.Direction ==
                StockMovementDirection.Increase
                    ? dto.Quantity
                    : -dto.Quantity;
        }

        if (delta < 0 &&
            product.StockQuantity < Math.Abs(delta))
        {
            throw new Exception(
                $"Not enough stock. Available: {product.StockQuantity}.");
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            product.StockQuantity += delta;

            _unitOfWork
                .GenaricRepository<Product>()
                .Update(product);

            var movement =
                new StockMovement
                {
                    ProductId =
                        product.Id,

                    Quantity =
                        delta,

                    MovementType =
                        dto.MovementType.ToString(),

                    MovementDate =
                        DateTime.UtcNow,

                    ReferenceType =
                        "Manual",

                    ReferenceId =
                        null,

                    Reason =
                        dto.Reason.Trim(),

                    Notes =
                        string.IsNullOrWhiteSpace(dto.Notes)
                            ? null
                            : dto.Notes.Trim()
                };

            await _unitOfWork
                .GenaricRepository<StockMovement>()
                .AddAsync(movement);

            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.CommitTransactionAsync();

            return new StockMovementDTO
            {
                Id =
                    movement.Id,

                ProductId =
                    product.Id,

                ProductName =
                    product.Name,

                Quantity =
                    movement.Quantity,

                MovementType =
                    movement.MovementType,

                MovementDate =
                    movement.MovementDate,

                ReferenceType =
                    movement.ReferenceType,

                ReferenceId =
                    movement.ReferenceId,

                Reason =
                    movement.Reason,

                Notes =
                    movement.Notes
            };
        }
        catch
        {
            await _unitOfWork
                .RollbackTransactionAsync();

            throw;
        }
    }
}