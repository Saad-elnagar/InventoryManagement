using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Mappings;

public static class CustomerMappingExtensions
{
    public static CustomerDTO ToDto(this Customer customer)
    {
        return new CustomerDTO
        {
            Id = customer.Id,
            Name = customer.CustomerName,
            ContactName = customer.ContactName,
            Phone = customer.Phone,
            Address = customer.Address,
            Email = customer.Email
        };
    }
    public static Customer ToEntity(this CustomerDTO dto)
    {
        return new Customer
        {
            CustomerName = dto.Name,
            ContactName = dto.ContactName,
            Phone = dto.Phone,
            Address = dto.Address,
            Email = dto.Email
        };
    }

    public static void ApplyTo(this CustomerDTO dto, Customer customer)
    {
        customer.CustomerName = dto.Name;
        customer.ContactName = dto.ContactName;
        customer.Phone = dto.Phone;
        customer.Address = dto.Address;
        customer.Email = dto.Email;
    }

}
