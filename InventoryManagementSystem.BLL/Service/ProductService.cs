using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Pagination;
using InventoryManagementSystem.DAL;
using InventoryManagementSystem.DAL.Entities;

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
            var repository = _unitOfWork.GenaricRepository<Product>();

            var products = await repository.GetAllAsync();

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
            var repository = _unitOfWork.GenaricRepository<Product>();

            var product = await repository.GetByIdAsync(id);

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
            var categoryRepository =
                _unitOfWork.GenaricRepository<Category>();

            var productRepository =
                _unitOfWork.GenaricRepository<Product>();

            var categoryExists = await categoryRepository.AnyAsync(
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

            await productRepository.AddAsync(product);

            await _unitOfWork.SaveChangesAsync();

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
            var productRepository =
                _unitOfWork.GenaricRepository<Product>();

            var categoryRepository =
                _unitOfWork.GenaricRepository<Category>();

            var product = await productRepository.GetByIdAsync(id);

            if (product == null)
                return false;

            var categoryExists = await categoryRepository.AnyAsync(
                c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new Exception("Category not found.");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.UnitPrice = dto.Price;
            product.StockQuantity = dto.Quantity;
            product.LowStockThreshold = dto.ReorderLevel;
            product.CategoryId = dto.CategoryId;

            productRepository.Update(product);

            await _unitOfWork.SaveChangesAsync();

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
            var productRepository =
                _unitOfWork.GenaricRepository<Product>();

            var product = await productRepository.GetByIdAsync(id);

            if (product == null)
                return false;

            var purchaseItemRepository =
                _unitOfWork.GenaricRepository<PurchaseItem>();

            var saleItemRepository =
                _unitOfWork.GenaricRepository<SaleItem>();

            var hasPurchaseHistory =
                await purchaseItemRepository.AnyAsync(x => x.ProductId == id);

            if (hasPurchaseHistory)
                throw new Exception(
                    "Cannot delete product because it has purchase history.");

            var hasSaleHistory =
                await saleItemRepository.AnyAsync(x => x.ProductId == id);

            if (hasSaleHistory)
                throw new Exception(
                    "Cannot delete product because it has sales history.");

            productRepository.Delete(product);

            await _unitOfWork.SaveChangesAsync();

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
            var repository = _unitOfWork.GenaricRepository<Product>();

            var products = await repository.GetWhereAsync(
                p => p.StockQuantity <= p.LowStockThreshold);

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
            throw new Exception(
                "Error while getting low stock products.", ex);
        }
    }

    public async Task<PaginationResult<ProductDTO>> GetPagedAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<PaginationResult<ProductDTO>> GetPagedAsync(
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var parameters = new PaginationParams
            {
                Page = page,
                PageSize = pageSize
            };

            var repository =
                _unitOfWork.GenaricRepository<Product>();

            var result = await repository.GetPagedAsync(
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
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting paginated products.", ex);
        }
    }
}