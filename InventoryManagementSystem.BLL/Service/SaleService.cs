using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Service;

public class SaleService : ISaleService
{
    private readonly IUnitOfWork _unitOfWork;

    public SaleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SaleDTO>> GetAllAsync()
    {
        try
        {
            var sales =
                await _unitOfWork
                    .GenaricRepository<Sale>()
                    .GetAllAsync();

            var customerIds =
                sales
                    .Select(s => s.CustomerId)
                    .Distinct()
                    .ToList();

            var customers =
                await _unitOfWork
                    .GenaricRepository<Customer>()
                    .GetWhereAsync(
                        c => customerIds.Contains(c.Id));

            var customerDictionary =
                customers.ToDictionary(
                    c => c.Id,
                    c => c.CustomerName);

            return sales
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new SaleDTO
                {
                    Id = s.Id,
                    CustomerId = s.CustomerId,

                    CustomerName =
                        customerDictionary.TryGetValue(
                            s.CustomerId,
                            out var customerName)
                            ? customerName
                            : null,

                    SaleDate = s.SaleDate,
                    TotalAmount = s.TotalAmount
                });
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting sales.",
                ex);
        }
    }

    public async Task<SaleDTO?> GetByIdAsync(int id)
    {
        try
        {
            var sale =
                await _unitOfWork
                    .GenaricRepository<Sale>()
                    .GetByIdAsync(id);

            if (sale == null)
                return null;

            var customer =
                await _unitOfWork
                    .GenaricRepository<Customer>()
                    .GetByIdAsync(
                        sale.CustomerId);

            var saleItems =
                await _unitOfWork
                    .GenaricRepository<SaleItem>()
                    .GetWhereAsync(
                        x => x.SaleId == id);

            var productIds =
                saleItems
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToList();

            var products =
                await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetWhereAsync(
                        x => productIds.Contains(x.Id));

            var productDictionary =
                products.ToDictionary(
                    x => x.Id,
                    x => x.Name);

            return new SaleDTO
            {
                Id = sale.Id,
                CustomerId = sale.CustomerId,

                CustomerName =
                    customer?.CustomerName,

                SaleDate = sale.SaleDate,
                TotalAmount = sale.TotalAmount,

                Items = saleItems
                    .OrderBy(x => x.Id)
                    .Select(x => new SaleItemDTO
                    {
                        Id = x.Id,
                        SaleId = x.SaleId,
                        ProductId = x.ProductId,

                        ProductName =
                            productDictionary.TryGetValue(
                                x.ProductId,
                                out var productName)
                                ? productName
                                : null,

                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice
                    })
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting sale.",
                ex);
        }
    }

    public async Task<SaleDTO> CreateAsync(
        SaleDTO dto)
    {
        try
        {
            var customerExists =
                await _unitOfWork
                    .GenaricRepository<Customer>()
                    .AnyAsync(
                        c => c.Id == dto.CustomerId);

            if (!customerExists)
            {
                throw new Exception(
                    "Customer not found.");
            }

            if (dto.Items == null ||
                dto.Items.Count == 0)
            {
                throw new Exception(
                    "Sale must contain at least one item.");
            }

            foreach (var item in dto.Items)
            {
                if (item.ProductId <= 0)
                {
                    throw new Exception(
                        "Please select a product.");
                }

                if (item.Quantity <= 0)
                {
                    throw new Exception(
                        "Quantity must be greater than zero.");
                }

                if (item.UnitPrice <= 0)
                {
                    throw new Exception(
                        "Unit price must be greater than zero.");
                }
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

                var totalQuantity =
                    group.Sum(x => x.Quantity);

                if (product.StockQuantity < totalQuantity)
                {
                    throw new Exception(
                        $"Not enough stock for product {product.Name}. " +
                        $"Available: {product.StockQuantity}.");
                }

                products[group.Key] = product;

                totalAmount +=
                    group.Sum(
                        x => x.Quantity * x.UnitPrice);
            }

            var sale = new Sale
            {
                CustomerId = dto.CustomerId,
                SaleDate = dto.SaleDate,
                TotalAmount = totalAmount
            };

            await _unitOfWork
                .GenaricRepository<Sale>()
                .AddAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                await _unitOfWork
                    .GenaricRepository<SaleItem>()
                    .AddAsync(
                        new SaleItem
                        {
                            SaleId = sale.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        });

                var product =
                    products[item.ProductId];

                product.StockQuantity -= item.Quantity;

                _unitOfWork
                    .GenaricRepository<Product>()
                    .Update(product);

                await _unitOfWork
                    .GenaricRepository<StockMovement>()
                    .AddAsync(
                        new StockMovement
                        {
                            ProductId = item.ProductId,
                            Quantity = -item.Quantity,

                            MovementType =
                                StockMovementType
                                    .Sale
                                    .ToString(),

                            MovementDate =
                                DateTime.UtcNow,

                            ReferenceType = "Sale",
                            ReferenceId = sale.Id,
                            Reason = "Product sold"
                        });
            }

            await _unitOfWork.SaveChangesAsync();

            dto.Id = sale.Id;
            dto.TotalAmount = totalAmount;

            return dto;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while creating sale.",
                ex);
        }
    }
}