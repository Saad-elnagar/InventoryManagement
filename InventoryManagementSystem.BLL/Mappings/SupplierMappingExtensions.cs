using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Mappings;

public static class SupplierMappingExtensions
{
    public static SupplierDTO ToDto(this Supplier supplier)
    {
        return new SupplierDTO
        {
            Id = supplier.Id,
            SupplierName = supplier.SupplierName,
            ContactName = supplier.ContactName,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address
        };
    }
}
