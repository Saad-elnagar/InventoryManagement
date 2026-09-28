using Exception = System.Exception;
using InventoryManagementSystem.BLL.DTOs;
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
            var saleRepository =
                _unitOfWork.GenaricRepository<Sale>();

            var customerRepository =
                _unitOfWork.GenaricRepository<Customer>();

            var sales = await saleRepository.GetAllAsync();

            var customerIds = sales
                .Select(s => s.CustomerId)
                .Distinct()
                .ToList();

            var customers = await customerRepository.GetWhereAsync(
                c => customerIds.Contains(c.Id));

            // Chage from List to Dic To Search Fast 
            var customerDictionary = customers
                .ToDictionary(c => c.Id, c => c.CustomerName);

            return sales.Select(s => new SaleDTO
            {
                Id = s.Id,
                CustomerId = s.CustomerId  ,
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
            var saleRepository =
                _unitOfWork.GenaricRepository<Sale>();

            var customerRepository =
                _unitOfWork.GenaricRepository<Customer>();

            var sale = await saleRepository.GetByIdAsync(id);

            if (sale == null)
                return null;

            var customer =
                await customerRepository.GetByIdAsync(sale.CustomerId);

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
            var customerRepository =
                _unitOfWork.GenaricRepository<Customer>();

            var saleRepository =
                _unitOfWork.GenaricRepository<Sale>();

            var saleItemRepository =
                _unitOfWork.GenaricRepository<SaleItem>();

            var productRepository =
                _unitOfWork.GenaricRepository<Product>();

            var customerExists =
                await customerRepository.AnyAsync(
                    c => c.Id == dto.CustomerId);

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

            var groupedItems = dto.Items
                .GroupBy(x => x.ProductId)
                .ToList();

            decimal totalAmount = 0;

            foreach (var group in groupedItems)
            {
                var product =
                    await productRepository.GetByIdAsync(group.Key);

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
                        $"Not enough stock for product {group.Key}.");
                }

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

            await saleRepository.AddAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };

                await saleItemRepository.AddAsync(saleItem);

                var product = products[item.ProductId];

                product.StockQuantity -= item.Quantity;

                productRepository.Update(product);
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
