using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Exceptions;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Mappings;
using InventoryManagementSystem.DAL;
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
                .Select(s => s.ToDto(
                    customerDictionary.GetValueOrDefault(s.CustomerId)));

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
                    .GetByIdAsync(sale.CustomerId);

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

            return sale.ToDto(
                customer?.CustomerName,
                saleItems,
                productDictionary);

        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting sale.",
                ex);
        }
    }

    public async Task<SaleDTO> CreateAsync(SaleDTO dto)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (dto.CustomerId <= 0)
                throw new BusinessRuleException("Please select a customer.");

            var customerExists =
                await _unitOfWork
                    .GenaricRepository<Customer>()
                    .AnyAsync(c => c.Id == dto.CustomerId);

            if (!customerExists)
                throw new BusinessRuleException("Customer not found.");

            if (dto.Items == null || dto.Items.Count == 0)
                throw new BusinessRuleException(
                    "Sale must contain at least one item.");

            foreach (var item in dto.Items)
            {
                if (item.ProductId <= 0)
                    throw new BusinessRuleException("Please select a product.");

                if (item.Quantity <= 0)
                    throw new BusinessRuleException(
                        "Quantity must be greater than zero.");

                if (item.UnitPrice <= 0)
                    throw new BusinessRuleException(
                        "Unit price must be greater than zero.");
            }

            var products = new Dictionary<int, Product>();
            decimal totalAmount = 0m;

            foreach (var group in dto.Items.GroupBy(x => x.ProductId))
            {
                var product =
                    await _unitOfWork
                        .GenaricRepository<Product>()
                        .GetByIdAsync(group.Key);

                if (product == null)
                    throw new BusinessRuleException(
                        $"Product {group.Key} not found.");

                var totalQuantity =
                    group.Sum(x => x.Quantity);

                if (product.StockQuantity < totalQuantity)
                {
                    throw new BusinessRuleException(
                        $"Not enough stock for product {product.Name}. " +
                        $"Available: {product.StockQuantity}.");
                }

                products[group.Key] = product;

                totalAmount += group.Sum(
                    x => x.Quantity * x.UnitPrice);
            }

            var sale = dto.ToEntity(totalAmount);

            await _unitOfWork
                .GenaricRepository<Sale>()
                .AddAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                await _unitOfWork
                    .GenaricRepository<SaleItem>()
                    .AddAsync(item.ToEntity(sale.Id));

                var product = products[item.ProductId];

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
                                StockMovementType.Sale.ToString(),
                            MovementDate = DateTime.UtcNow,
                            ReferenceType = "Sale",
                            ReferenceId = sale.Id,
                            Reason = "Product sold"
                        });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            dto.Id = sale.Id;
            dto.TotalAmount = totalAmount;

            return dto;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
