using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Mappings;

public static class PurchaseMappingExtensions
{
    public static PurchaseDTO ToDto(
        this Purchase purchase,
        string? supplierName = null,
        IEnumerable<PurchaseItem>? items = null,
        IReadOnlyDictionary<int, string>? productNames = null)
    {
        return new PurchaseDTO
        {
            Id = purchase.Id,
            SupplierId = purchase.SupplierId,
            SupplierName = supplierName,
            PurchaseDate = purchase.PurchaseDate,
            TotalAmount = purchase.TotalAmount,
            Items = items?
                .OrderBy(item => item.Id)
                .Select(item => item.ToDto(productNames))
                .ToList() ?? []
        };
    }

    public static PurchaseItemDTO ToDto(
        this PurchaseItem item,
        IReadOnlyDictionary<int, string>? productNames = null)
    {
        var productName = productNames is not null &&
                          productNames.TryGetValue(item.ProductId, out var name)
            ? name
            : null;

        return new PurchaseItemDTO
        {
            Id = item.Id,
            PurchaseId = item.PurchaseId,
            ProductId = item.ProductId,
            ProductName = productName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitCost
        };
    }
}
