using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Mappings;
using InventoryManagementSystem.DAL;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Service;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CustomerDTO>> GetAllAsync()
    {
        var customers =
            await _unitOfWork.GenaricRepository<Customer>()
                .GetAllAsync();

        return customers
            .Select(c => c.ToDto())
            .ToList();
    }

    public async Task<CustomerDTO?> GetByIdAsync(int id)
    {
        var customer =
            await _unitOfWork.GenaricRepository<Customer>()
                .GetByIdAsync(id);

        if (customer == null)
            return null;

        return customer.ToDto();
    }

    public async Task<int> CreateAsync(CustomerDTO dto)
    {
        var customer = new Customer
        {
            CustomerName = dto.Name,
            ContactName = dto.ContactName,
            Phone = dto.Phone,
            Address = dto.Address,
            Email = dto.Email
        };

        await _unitOfWork.GenaricRepository<Customer>()
            .AddAsync(customer);

        await _unitOfWork.SaveChangesAsync();

        return customer.Id;
    }

    public async Task<int> UpdateAsync(int id, CustomerDTO dto)
    {
        var customer =
            await _unitOfWork.GenaricRepository<Customer>()
                .GetByIdAsync(id);

        if (customer == null)
            return 0;

        customer.CustomerName = dto.Name;
        customer.ContactName = dto.ContactName;
        customer.Phone = dto.Phone;
        customer.Address = dto.Address;
        customer.Email = dto.Email;

        _unitOfWork.GenaricRepository<Customer>()
            .Update(customer);

        await _unitOfWork.SaveChangesAsync();

        return 1;
    }

    public async Task<int> DeleteAsync(int id)
    {
        var customer =
            await _unitOfWork.GenaricRepository<Customer>()
                .GetByIdAsync(id);

        if (customer == null)
            return 0;

        _unitOfWork.GenaricRepository<Customer>()
            .Delete(customer);

        await _unitOfWork.SaveChangesAsync();

        return 1;
    }
}