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
}
