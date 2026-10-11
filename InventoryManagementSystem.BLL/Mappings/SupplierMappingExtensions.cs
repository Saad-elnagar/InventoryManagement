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
    public static Supplier ToEntity(this SupplierDTO dto)
    {
        return new Supplier
        {
            SupplierName = dto.SupplierName,
            ContactName = dto.ContactName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address
        };
    }

    public static void ApplyTo(this SupplierDTO dto, Supplier supplier)
    {
        supplier.SupplierName = dto.SupplierName;
        supplier.ContactName = dto.ContactName;
        supplier.Phone = dto.Phone;
        supplier.Email = dto.Email;
        supplier.Address = dto.Address;
    }

}
