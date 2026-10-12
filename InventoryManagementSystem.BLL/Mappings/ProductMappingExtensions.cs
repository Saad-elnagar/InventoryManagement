using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Mappings;

public static class ProductMappingExtensions
{
    public static ProductDTO ToDto(
        this Product product,
        string? categoryName = null)
    {
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
            CategoryName = categoryName
        };
    }
    public static Product ToEntity(this ProductDTO dto)
    {
        return new Product
        {
            Name = dto.Name.Trim(),
            Sku = dto.SKU.Trim(),
            Description = dto.Description?.Trim(),
            UnitPrice = dto.Price,
            StockQuantity = dto.Quantity,
            LowStockThreshold = dto.ReorderLevel,
            CategoryId = dto.CategoryId
        };
    }

    public static void ApplyTo(this ProductDTO dto, Product product)
    {
        product.Name = dto.Name.Trim();
        product.Sku = dto.SKU.Trim();
        product.Description = dto.Description?.Trim();
        product.UnitPrice = dto.Price;
        product.StockQuantity = dto.Quantity;
        product.LowStockThreshold = dto.ReorderLevel;
        product.CategoryId = dto.CategoryId;
    }

}
