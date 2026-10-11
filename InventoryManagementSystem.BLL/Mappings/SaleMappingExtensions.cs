using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Mappings;

public static class SaleMappingExtensions
{
    public static SaleDTO ToDto(
        this Sale sale,
        string? customerName = null,
        IEnumerable<SaleItem>? items = null,
        IReadOnlyDictionary<int, string>? productNames = null)
    {
        return new SaleDTO
        {
            Id = sale.Id,
            CustomerId = sale.CustomerId,
            CustomerName = customerName,
            SaleDate = sale.SaleDate,
            TotalAmount = sale.TotalAmount,
            Items = items?
                .OrderBy(item => item.Id)
                .Select(item => item.ToDto(productNames))
                .ToList() ?? []
        };
    }

    public static SaleItemDTO ToDto(
        this SaleItem item,
        IReadOnlyDictionary<int, string>? productNames = null)
    {
        var productName = productNames is not null &&
                          productNames.TryGetValue(item.ProductId, out var name)
            ? name
            : null;

        return new SaleItemDTO
        {
            Id = item.Id,
            SaleId = item.SaleId,
            ProductId = item.ProductId,
            ProductName = productName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        };
    }
}
