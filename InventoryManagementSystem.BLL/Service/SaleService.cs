using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

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
                await _unitOfWork.GenaricRepository<Sale>().GetAllAsync();

            return sales.Select(s => new SaleDTO
            {
                Id = s.Id,
                CustomerId = s.Id,
                CustomerName = s.CustomerInfo,
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
            var sale = await _unitOfWork.GenaricRepository<Sale>().GetByIdAsync(id);
            var saleDto = new SaleDTO
            {
                CustomerName = sale.CustomerInfo,
                SaleDate = sale.SaleDate,
                TotalAmount = sale.TotalAmount
            };
            return saleDto;
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
            var customerExists =
                await _unitOfWork.GenaricRepository<Customer>().AnyAsync(
                    c => c.Id == dto.CustomerId);

            if (!customerExists)
                throw new Exception("Customer not found.");

            if (dto.Items == null || !dto.Items.Any())
                throw new Exception("Sale must contain items.");

            decimal totalAmount = 0;

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    throw new Exception(
                        "Quantity must be greater than zero.");

                if (item.UnitPrice <= 0)
                    throw new Exception(
                        "Unit price must be greater than zero.");

                var product =
                    await _unitOfWork.GenaricRepository<Product>().GetByIdAsync(
                        item.ProductId);

                if (product == null)
                    throw new Exception(
                        $"Product {item.ProductId} not found.");

                if (product.StockQuantity < item.Quantity)
                    throw new Exception(
                        $"Not enough stock for product {item.ProductId}.");

                totalAmount +=
                    item.Quantity * item.UnitPrice;
            }

            var sale = new Sale
            {
                CustomerId = dto.CustomerId,
                SaleDate = dto.SaleDate,
                TotalAmount = totalAmount
            };

             _unitOfWork.GenaricRepository<Sale>().AddAsync(sale);

            foreach (var item in dto.Items)
            {
                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };

                 _unitOfWork.GenaricRepository<SaleItem>().AddAsync(saleItem);

                var product =
                    await _unitOfWork.GenaricRepository<Product>().GetByIdAsync(
                        item.ProductId);

                product!.StockQuantity -= item.Quantity;

                _unitOfWork.GenaricRepository<Product>().Update(product);
            }

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