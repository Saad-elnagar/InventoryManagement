using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Pagination;
using InventoryManagementSystem.DAL;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.BLL.Enums;
using Microsoft.EntityFrameworkCore;

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
            var products =
                await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetAllAsync();

            return products.Select(p => new ProductDTO
            {
                Id = p.Id,
                SKU = p.Sku,
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
                "Error while getting products.",
                ex);
        }
    }

    public async Task<ProductDTO?> GetByIdAsync(int id)
    {
        var product =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetByIdAsync(id);

        if (product == null)
            return null;

        var category =
            await _unitOfWork
                .GenaricRepository<Category>()
                .GetByIdAsync(product.CategoryId);

        return new ProductDTO
        {
            Id = product.Id,
            SKU = product.Sku,
            Name = product.Name,
            Description = product.Description,
            Price = product.UnitPrice,
            Quantity = product.StockQuantity,
            ReorderLevel = product.LowStockThreshold,
            CategoryId = product.CategoryId,
            CategoryName = category?.Name
        };
    }

    public async Task<ProductDTO> CreateAsync(ProductDTO dto)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var sku = dto.SKU.Trim();
            var name = dto.Name.Trim();
            var description = dto.Description?.Trim();

            if (string.IsNullOrWhiteSpace(sku))
                throw new Exception("SKU is required.");

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Product name is required.");

            var categoryExists =
                await _unitOfWork
                    .GenaricRepository<Category>()
                    .AnyAsync(c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new Exception("Category not found.");

            var skuExists =
                await _unitOfWork
                    .GenaricRepository<Product>()
                    .AnyAsync(p => p.Sku == sku);

            if (skuExists)
                throw new Exception("SKU already exists.");

            var product = new Product
            {
                Name = name,
                Sku = sku,
                Description = description,
                UnitPrice = dto.Price,
                StockQuantity = dto.Quantity,
                LowStockThreshold = dto.ReorderLevel,
                CategoryId = dto.CategoryId
            };

            await _unitOfWork
                .GenaricRepository<Product>()
                .AddAsync(product);

            await _unitOfWork.SaveChangesAsync();

            if (product.StockQuantity > 0)
            {
                await _unitOfWork
                    .GenaricRepository<StockMovement>()
                    .AddAsync(
                        new StockMovement
                        {
                            ProductId = product.Id,
                            Quantity = product.StockQuantity,
                            MovementType =
                                StockMovementType.OpeningStock.ToString(),
                            MovementDate = DateTime.UtcNow,
                            ReferenceType = "Manual",
                            ReferenceId = null,
                            Reason = "Opening stock"
                        });

                await _unitOfWork.SaveChangesAsync();
            }

            await _unitOfWork.CommitTransactionAsync();

            dto.Id = product.Id;
            dto.SKU = product.Sku;
            dto.Name = product.Name;
            dto.Description = product.Description;
            dto.Quantity = product.StockQuantity;
            dto.Price = product.UnitPrice;
            dto.ReorderLevel = product.LowStockThreshold;
            dto.CategoryId = product.CategoryId;

            return dto;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
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

            var product =
                await productRepository.GetByIdAsync(id);

            if (product == null)
                return false;

            var sku = dto.SKU.Trim();
            var name = dto.Name.Trim();

            if (string.IsNullOrWhiteSpace(sku))
                throw new Exception("SKU is required.");

            var categoryExists =
                await categoryRepository.AnyAsync(
                    c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new Exception("Category not found.");

            var duplicateSku =
                await productRepository.AnyAsync(
                    p => p.Id != id && p.Sku == sku);

            if (duplicateSku)
                throw new Exception("SKU already exists.");

            product.Name = name;
            product.Sku = sku;
            product.Description = dto.Description?.Trim();
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
            throw new Exception(
                "Error while updating product.",
                ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var productRepository =
                _unitOfWork.GenaricRepository<Product>();

            var product =
                await productRepository.GetByIdAsync(id);

            if (product == null)
                return false;

            var purchaseItemRepository =
                _unitOfWork.GenaricRepository<PurchaseItem>();

            var saleItemRepository =
                _unitOfWork.GenaricRepository<SaleItem>();

            var hasPurchaseHistory =
                await purchaseItemRepository.AnyAsync(
                    x => x.ProductId == id);

            if (hasPurchaseHistory)
                throw new Exception(
                    "Cannot delete product because it has purchase history.");

            var hasSaleHistory =
                await saleItemRepository.AnyAsync(
                    x => x.ProductId == id);

            if (hasSaleHistory)
                throw new Exception(
                    "Cannot delete product because it has sales history.");

            productRepository.Delete(product);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while deleting product.",
                ex);
        }
    }

    public async Task<IEnumerable<ProductDTO>> GetLowStockAsync()
    {
        try
        {
            var products =
                await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetWhereAsync(
                        p => p.StockQuantity <= p.LowStockThreshold);

            return products.Select(p => new ProductDTO
            {
                Id = p.Id,
                SKU = p.Sku,
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
                "Error while getting low stock products.",
                ex);
        }
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

            var result =
                await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetPagedAsync(
                        parameters.Page,
                        parameters.PageSize);

            var data =
                result.Items
                    .Select(p => new ProductDTO
                    {
                        Id = p.Id,
                        SKU = p.Sku,
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
                "Error while getting paginated products.",
                ex);
        }
    }

    public async Task<PaginationResult<ProductDTO>> SearchAsync(
        string search,
        int page = 1,
        int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(search))
            return await GetPagedAsync(page, pageSize);

        search = search.Trim();

        var result =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetPagedAsync(
                    page,
                    pageSize,
                    p =>
                        EF.Functions.Like(
                            EF.Functions.Collate(
                                p.Name,
                                "SQL_Latin1_General_CP1_CI_AS"),
                            $"%{search}%")
                        ||
                        (p.Sku != null &&
                         EF.Functions.Like(
                             EF.Functions.Collate(
                                 p.Sku,
                                 "SQL_Latin1_General_CP1_CI_AS"),
                             $"%{search}%"))
                        ||
                        (p.Description != null &&
                         EF.Functions.Like(
                             EF.Functions.Collate(
                                 p.Description,
                                 "SQL_Latin1_General_CP1_CI_AS"),
                             $"%{search}%"))
                );

        var data =
            result.Items
                .Select(p => new ProductDTO
                {
                    Id = p.Id,
                    SKU = p.Sku,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.UnitPrice,
                    Quantity = p.StockQuantity,
                    ReorderLevel = p.LowStockThreshold,
                    CategoryId = p.CategoryId
                })
                .ToList();

        var parameters = new PaginationParams
        {
            Page = page,
            PageSize = pageSize
        };

        return PaginationHelper.Create(
            data,
            parameters,
            result.TotalCount);
    }
}
