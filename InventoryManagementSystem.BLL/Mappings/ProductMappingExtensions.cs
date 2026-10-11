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
}
