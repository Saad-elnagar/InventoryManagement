using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDTO>> GetAllAsync();
    Task<CustomerDTO?> GetByIdAsync(int id);
  Task <int> CreateAsync(CustomerDTO dto);
   Task<int> UpdateAsync(int id, CustomerDTO dto);
   Task<int> DeleteAsync(int id);
}