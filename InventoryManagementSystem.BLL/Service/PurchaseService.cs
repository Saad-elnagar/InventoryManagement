using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Service;

public class PurchaseService : IPurchaseService
{
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PurchaseDTO>> GetAllAsync()
    {
        try
        {
            var purchases =
                await _unitOfWork
                    .GenaricRepository<Purchase>()
                    .GetAllAsync();

            var supplierIds = purchases
                .Select(p => p.SupplierId)
                .Distinct()
                .ToList();

            var suppliers =
                await _unitOfWork
                    .GenaricRepository<Supplier>()
                    .GetWhereAsync(
                        s => supplierIds.Contains(s.Id));

            var supplierDictionary = suppliers
                .ToDictionary(
                    s => s.Id,
                    s => s.SupplierName);

            return purchases
                .OrderByDescending(p => p.PurchaseDate)
                .Select(p => new PurchaseDTO
                {
                    Id = p.Id,
                    SupplierId = p.SupplierId,
                    SupplierName =
                        supplierDictionary.TryGetValue(
                            p.SupplierId,
                            out var supplierName)
                            ? supplierName
                            : null,
                    PurchaseDate = p.PurchaseDate,
                    TotalAmount = p.TotalAmount
                });
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting purchases.",
                ex);
        }
    }

    public async Task<PurchaseDTO?> GetByIdAsync(int id)
    {
        try
        {
            var purchase =
                await _unitOfWork
                    .GenaricRepository<Purchase>()
                    .GetByIdAsync(id);

            if (purchase == null)
                return null;

            var supplier =
                await _unitOfWork
                    .GenaricRepository<Supplier>()
                    .GetByIdAsync(
                        purchase.SupplierId);

            var purchaseItems =
                await _unitOfWork
                    .GenaricRepository<PurchaseItem>()
                    .GetWhereAsync(
                        x => x.PurchaseId == id);

            var productIds = purchaseItems
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products =
                await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetWhereAsync(
                        x => productIds.Contains(x.Id));

            var productDictionary = products
                .ToDictionary(
                    x => x.Id,
                    x => x.Name);

            return new PurchaseDTO
            {
                Id = purchase.Id,
                SupplierId = purchase.SupplierId,
                SupplierName = supplier?.SupplierName,
                PurchaseDate = purchase.PurchaseDate,
                TotalAmount = purchase.TotalAmount,

                Items = purchaseItems
                    .OrderBy(x => x.Id)
                    .Select(x => new PurchaseItemDTO
                    {
                        Id = x.Id,
                        PurchaseId = x.PurchaseId,
                        ProductId = x.ProductId,
                        ProductName =
                            productDictionary.TryGetValue(
                                x.ProductId,
                                out var productName)
                                ? productName
                                : null,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitCost
                    })
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting purchase.",
                ex);
        }
    }

    public async Task<PurchaseDTO> CreateAsync(
        PurchaseDTO dto)
    {
        try
        {
            var supplierExists =
                await _unitOfWork
                    .GenaricRepository<Supplier>()
                    .AnyAsync(
                        s => s.Id == dto.SupplierId);

            if (!supplierExists)
                throw new Exception(
                    "Supplier not found.");

            if (dto.Items == null ||
                dto.Items.Count == 0)
            {
                throw new Exception(
                    "Purchase must contain at least one item.");
            }

            foreach (var item in dto.Items)
            {
                if (item.ProductId <= 0)
                    throw new Exception(
                        "Please select a product.");

                if (item.Quantity <= 0)
                    throw new Exception(
                        "Quantity must be greater than zero.");

                if (item.UnitPrice <= 0)
                    throw new Exception(
                        "Unit cost must be greater than zero.");
            }

            var products =
                new Dictionary<int, Product>();

            decimal totalAmount = 0;

            foreach (var group in dto.Items.GroupBy(
                         x => x.ProductId))
            {
                var product =
                    await _unitOfWork
                        .GenaricRepository<Product>()
                        .GetByIdAsync(group.Key);

                if (product == null)
                {
                    throw new Exception(
                        $"Product {group.Key} not found.");
                }

                products[group.Key] = product;

                totalAmount += group.Sum(
                    x => x.Quantity * x.UnitPrice);
            }

            var purchase = new Purchase
            {
                SupplierId = dto.SupplierId,
                PurchaseDate = dto.PurchaseDate,
                TotalAmount = totalAmount
            };

            await _unitOfWork
                .GenaricRepository<Purchase>()
                .AddAsync(purchase);

            await _unitOfWork.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                await _unitOfWork
                    .GenaricRepository<PurchaseItem>()
                    .AddAsync(
                        new PurchaseItem
                        {
                            PurchaseId = purchase.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitCost = item.UnitPrice
                        });

                var product =
                    products[item.ProductId];

                product.StockQuantity += item.Quantity;

                _unitOfWork
                    .GenaricRepository<Product>()
                    .Update(product);

                await _unitOfWork
                    .GenaricRepository<StockMovement>()
                    .AddAsync(
                        new StockMovement
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            MovementType =
                                StockMovementType
                                    .Purchase
                                    .ToString(),
                            MovementDate =
                                DateTime.UtcNow,
                            ReferenceType = "Purchase",
                            ReferenceId = purchase.Id,
                            Reason = "Purchase received"
                        });
            }

            await _unitOfWork.SaveChangesAsync();

            dto.Id = purchase.Id;
            dto.TotalAmount = totalAmount;

            return dto;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while creating purchase.",
                ex);
        }
    }
}