using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Exceptions;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Mappings;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.DAL;
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
            var from = fromDate?.Date;
            var toExclusive = toDate?.Date.AddDays(1);
            var type = string.IsNullOrWhiteSpace(movementType)
                ? null
                : movementType.Trim();

            Expression<Func<StockMovement, bool>> predicate = movement =>
                (!productId.HasValue || movement.ProductId == productId.Value) &&
                (type == null ||
                 EF.Functions.Collate(
                     movement.MovementType,
                     "SQL_Latin1_General_CP1_CI_AS") == type) &&
                (!from.HasValue || movement.MovementDate >= from.Value) &&
                (!toExclusive.HasValue || movement.MovementDate < toExclusive.Value);

            var movements = await _unitOfWork
                .GenaricRepository<StockMovement>()
                .GetWhereAsync(predicate);

            movements = movements
                .OrderByDescending(x => x.MovementDate)
                .ThenByDescending(x => x.Id);

            var productIds = movements
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = productIds.Count == 0
                ? Enumerable.Empty<Product>()
                : await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetWhereAsync(x => productIds.Contains(x.Id));

            var productDictionary = products.ToDictionary(
                x => x.Id,
                x => x.Name);

            return movements.Select(
                x => x.ToDto(productDictionary.GetValueOrDefault(x.ProductId)));

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

            return movement.ToDto(product?.Name);

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

        if (!dto.MovementType.HasValue ||
            !manualTypes.Contains(dto.MovementType.Value))
        {
            throw new BusinessRuleException(
                "Purchase and Sale movements are created automatically.");
        }

        if (dto.Quantity <= 0)
        {
            throw new BusinessRuleException(
                "Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new BusinessRuleException(
                "Reason is required.");
        }

        var product =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetByIdAsync(
                    dto.ProductId);

        if (product == null)
        {
            throw new BusinessRuleException(
                "Product not found.");
        }

        var movementType = dto.MovementType.Value;

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
                movementType))
        {
            delta =
                -dto.Quantity;
        }
        else if (increaseTypes.Contains(
                     movementType))
        {
            delta =
                dto.Quantity;
        }
        else if (movementType ==
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
            throw new BusinessRuleException(
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
                        movementType.ToString(),

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

            return movement.ToDto(product.Name);

        }
        catch
        {
            await _unitOfWork
                .RollbackTransactionAsync();

            throw;
        }
    }
}