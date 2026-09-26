using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

namespace InventoryManagementSystem.BLL.Service;

public class ProductService : IProductService
{
    private readonly IGenaricRepository<Product> _productRepository;
    private readonly IGenaricRepository<Category> _categoryRepository;

    public ProductService(
        IGenaricRepository<Product> productRepository,
        IGenaricRepository<Category> categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<ProductDTO>> GetAllAsync()
    {
        try
        {
            var products = await _productRepository.GetAllAsync();

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
            var product = await _productRepository.GetByIdAsync(id);

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
                await _categoryRepository.AnyAsync(
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

            await _productRepository.AddAsync(product);

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
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
                return false;

            var categoryExists =
                await _categoryRepository.AnyAsync(
                    c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new Exception("Category not found.");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.UnitPrice = dto.Price;
            product.StockQuantity = dto.Quantity;
            product.LowStockThreshold = dto.ReorderLevel;
            product.CategoryId = dto.CategoryId;

            _productRepository.Update(product);

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
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null)
                return false;

            _productRepository.Delete(product);

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
            var products = await _productRepository.GetAllAsync();

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
}