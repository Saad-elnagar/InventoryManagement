using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Mappings;
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

            return products.Select(p => p.ToDto());
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

        return product.ToDto(category?.Name);
    }

    public async Task<ProductDTO> CreateAsync(ProductDTO dto)
    {
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var sku = dto.SKU?.Trim() ?? string.Empty;
            var name = dto.Name?.Trim() ?? string.Empty;
            var description = dto.Description?.Trim();

            if (string.IsNullOrWhiteSpace(sku))
                throw new Exception("SKU is required.");

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Product name is required.");

            if (dto.Price <= 0)
                throw new Exception("Price must be greater than zero.");

            if (dto.Quantity < 0)
                throw new Exception("Quantity cannot be negative.");

            if (dto.ReorderLevel < 0)
                throw new Exception("Reorder level cannot be negative.");

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

            var product = dto.ToEntity();

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

            return product.ToDto();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, ProductDTO dto)
    {
        var transactionStarted = false;

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

            var sku = dto.SKU?.Trim() ?? string.Empty;
            var name = dto.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(sku))
                throw new Exception("SKU is required.");

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Product name is required.");

            if (dto.Price <= 0)
                throw new Exception("Price must be greater than zero.");

            if (dto.Quantity < 0)
                throw new Exception("Quantity cannot be negative.");

            if (dto.ReorderLevel < 0)
                throw new Exception("Reorder level cannot be negative.");

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

            var stockDelta = dto.Quantity - product.StockQuantity;

            await _unitOfWork.BeginTransactionAsync();
            transactionStarted = true;

            dto.ApplyTo(product);

            productRepository.Update(product);

            if (stockDelta != 0)
            {
                await _unitOfWork
                    .GenaricRepository<StockMovement>()
                    .AddAsync(
                        new StockMovement
                        {
                            ProductId = product.Id,
                            Quantity = stockDelta,
                            MovementType = StockMovementType.Adjustment.ToString(),
                            MovementDate = DateTime.UtcNow,
                            ReferenceType = "Manual",
                            ReferenceId = null,
                            Reason = "Stock quantity adjusted through product edit"
                        });
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            transactionStarted = false;

            return true;
        }
        catch (Exception ex)
        {
            if (transactionStarted)
                await _unitOfWork.RollbackTransactionAsync();

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

            var hasStockMovementHistory =
                await _unitOfWork
                    .GenaricRepository<StockMovement>()
                    .AnyAsync(movement => movement.ProductId == id);

            if (hasStockMovementHistory)
                throw new Exception(
                    "Cannot delete product because it has stock movement history.");

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

            return products.Select(p => p.ToDto());
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

            parameters.Normalize();

            var result =
                await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetPagedAsync(
                        parameters.Page,
                        parameters.PageSize);

            var data =
                result.Items
                    .Select(p => p.ToDto())
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

        var parameters = new PaginationParams
        {
            Page = page,
            PageSize = pageSize
        };
        parameters.Normalize();

        var result =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetPagedAsync(
                    parameters.Page,
                    parameters.PageSize,
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
                .Select(p => p.ToDto())
                .ToList();



        return PaginationHelper.Create(
            data,
            parameters,
            result.TotalCount);
    }
}
