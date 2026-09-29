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
            // get all sales
            var sales =
                await _unitOfWork.GenaricRepository<Sale>()
                    .GetAllAsync();

            // get ids of customers who have made sales 
            var customerIds = sales
                .Select(s => s.CustomerId)
                .Distinct()
                .ToHashSet();
            
            var customers =
                await _unitOfWork.GenaricRepository<Customer>()
                    .GetWhereAsync(c => customerIds.Contains(c.Id));
             
            var customerDictionary = customers
                .ToDictionary(c => c.Id, c => c.CustomerName);

            return sales.Select(s => new SaleDTO
            {
                Id = s.Id,
                CustomerId = s.CustomerId,
                CustomerName = customerDictionary.TryGetValue(
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
            throw new Exception("Error while getting sales.", ex);
        }
    }

    public async Task<SaleDTO?> GetByIdAsync(int id)
    {
        try
        {
            var sale =
                await _unitOfWork.GenaricRepository<Sale>()
                    .GetByIdAsync(id);

            if (sale == null)
                return null;

            var customer =
                await _unitOfWork.GenaricRepository<Customer>()
                    .GetByIdAsync(sale.CustomerId);

            return new SaleDTO
            {
                Id = sale.Id,
                CustomerId = sale.CustomerId,
                CustomerName = customer?.CustomerName,
                SaleDate = sale.SaleDate,
                TotalAmount = sale.TotalAmount
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Error while getting sale.", ex);
        }
    }

    public async Task<SaleDTO> CreateAsync(SaleDTO dto)
    {
        try
        {
            // check is exit 
            var customerExists =
                await _unitOfWork.GenaricRepository<Customer>()
                    .AnyAsync(c => c.Id == dto.CustomerId);

            if (!customerExists)
                throw new Exception("Customer not found.");

            if (dto.Items == null || !dto.Items.Any())
                throw new Exception("Sale must contain items.");

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    throw new Exception(
                        "Quantity must be greater than zero.");

                if (item.UnitPrice <= 0)
                    throw new Exception(
                        "Unit price must be greater than zero.");
            }
            
            var products = new Dictionary<int, Product>();
            decimal totalAmount = 0;

            foreach (var group in dto.Items.GroupBy(x => x.ProductId))
            {
                var product =
                    await _unitOfWork.GenaricRepository<Product>()
                        .GetByIdAsync(group.Key);

                if (product == null)
                    throw new Exception(
                        $"Product {group.Key} not found.");

                var totalQuantity =
                    group.Sum(x => x.Quantity);

                if (product.StockQuantity < totalQuantity)
                    throw new Exception(
                        $"Not enough stock for product {group.Key}.");

                products[group.Key] = product;

                totalAmount += group.Sum(
                    x => x.Quantity * x.UnitPrice);
            }

            var sale = new Sale
            {
                CustomerId = dto.CustomerId,
                SaleDate = dto.SaleDate,
                TotalAmount = totalAmount
            };

            await _unitOfWork.GenaricRepository<Sale>()
                .AddAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                await _unitOfWork.GenaricRepository<SaleItem>()
                    .AddAsync(new SaleItem
                    {
                        SaleId = sale.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });

                var product = products[item.ProductId];

                product.StockQuantity -= item.Quantity;

                _unitOfWork.GenaricRepository<Product>()
                    .Update(product);

                await _unitOfWork.GenaricRepository<StockMovement>()
                    .AddAsync(new StockMovement
                    {
                        ProductId = item.ProductId,
                        Quantity = -item.Quantity,
                        MovementType =
                            StockMovementType.Sale.ToString(),
                        MovementDate = DateTime.UtcNow,
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
            throw new Exception("Error while creating sale.", ex);
        }
    }
}