using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Pagination;
using InventoryManagementSystem.DAL;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

namespace InventoryManagementSystem.BLL.Service;

public class ProductService : IProductService
{
  private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)

    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductDTO>> GetAllAsync()
    {
        try
        {
            var products = await _unitOfWork.GenaricRepository<Product>().GetAllAsync();

            return products.Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.UnitPrice,
                Quantity = p.StockQuantity,
                ReorderLevel = p.LowStockThreshold,
                CategoryId = p.CategoryId
            });
        }
        catch (Exception ex)
        {
            throw new Exception("Error while getting products.", ex);
        }
    }

    public async Task<ProductDTO?> GetByIdAsync(int id)
    {
        try
        {
            var product = await _unitOfWork.GenaricRepository<Product>().GetByIdAsync(id);

            if (product == null)
                return null;

            return new ProductDTO
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.UnitPrice,
                Quantity = product.StockQuantity,
                ReorderLevel = product.LowStockThreshold,
                CategoryId = product.CategoryId
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Error while getting product.", ex);
        }
    }

    public async Task<ProductDTO> CreateAsync(ProductDTO dto)
    {
        try
        {
            var categoryExists =
                await _unitOfWork.GenaricRepository<Category>().AnyAsync(
                    c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new Exception("Category not found.");

            var product = new Product
            {
                Name = dto.Name,
                
               Description = dto.Description,
                UnitPrice = dto.Price,
                StockQuantity = dto.Quantity,
                LowStockThreshold = dto.ReorderLevel,
                CategoryId = dto.CategoryId
            };

             _unitOfWork.GenaricRepository<Product>().AddAsync(product);

            dto.Id = product.Id;

            return dto;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while creating product.", ex);
        }
    }

    public async Task<bool> UpdateAsync(int id, ProductDTO dto)
    {
        try
        {
            var product = await _unitOfWork.GenaricRepository<Product>().GetByIdAsync(id);

            if (product == null)
                return false;

            var categoryExists =
                await _unitOfWork.GenaricRepository<Category>().AnyAsync(
                    c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new Exception("Category not found.");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.UnitPrice = dto.Price;
            product.StockQuantity = dto.Quantity;
            product.LowStockThreshold = dto.ReorderLevel;
            product.CategoryId = dto.CategoryId;

            _unitOfWork.GenaricRepository<Product>().Update(product);

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while updating product.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var product = await _unitOfWork.GenaricRepository<Product>().GetByIdAsync(id);

            if (product == null)
                return false;

            _unitOfWork.GenaricRepository<Product>().Delete(product);

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while deleting product.", ex);
        }
    }

    public async Task<IEnumerable<ProductDTO>> GetLowStockAsync()
    {
        try
        {
            var products = await _unitOfWork.GenaricRepository<Product>().GetAllAsync();

            return products
                .Where(p => p.StockQuantity <= p.LowStockThreshold)
                .Select(p => new ProductDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.UnitPrice,
                    Quantity = p.StockQuantity,
                    ReorderLevel = p.LowStockThreshold,
                    CategoryId = p.CategoryId
                });
        }
        catch (Exception ex)
        {
            throw new Exception("Error while getting low stock products.", ex);
        }
    }

    public async Task<PaginationResult<ProductDTO>> GetPagedAsync(int page = 1, int pageSize = 10)
    {
        var parameters = new PaginationParams
        {
            Page = page,
            PageSize = pageSize
        };

        var result = await _unitOfWork.GenaricRepository<Product>().GetPagedAsync(
            parameters.Page,
            parameters.PageSize);

        var data = result.Items
            .Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.UnitPrice,
                Quantity = p.StockQuantity,
                ReorderLevel = p.LowStockThreshold,
                CategoryId = p.CategoryId
            })
            .ToList();

        return PaginationHelper.Create(
            data,
            parameters,
            result.TotalCount);
    }
}