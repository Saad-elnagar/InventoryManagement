using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
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
        try
        {
            var customers =
                await _unitOfWork.GenaricRepository<Customer>()
                    .GetAllAsync();

            return customers
                .Select(c => new CustomerDTO
                {
                    Id = c.Id,
                    Name = c.CustomerName,
                    Phone = c.Phone,
                    Address = c.Address,
                    Email = c.Email
                })
                .ToList();
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting customers.",
                ex);
        }
    }

    public async Task<CustomerDTO?> GetByIdAsync(int id)
    {
        try
        {
            var customer =
                await _unitOfWork.GenaricRepository<Customer>()
                    .GetByIdAsync(id);

            if (customer == null)
                return null;

            return new CustomerDTO
            {
                Id = customer.Id,
                Name = customer.CustomerName,
                Phone = customer.Phone,
                Address = customer.Address,
                Email = customer.Email
            };
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while getting customer.",
                ex);
        }
    }

    public async Task<int> CreateAsync(CustomerDTO dto)
    {
        try
        {
            var customer = new Customer
            {
                CustomerName = dto.Name,
                Phone = dto.Phone,
                Address = dto.Address,
                Email = dto.Email
            };

            await _unitOfWork.GenaricRepository<Customer>()
                .AddAsync(customer);

            await _unitOfWork.SaveChangesAsync();

            return customer.Id;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while creating customer.",
                ex);
        }
    }

    public async Task<int> UpdateAsync(int id, CustomerDTO dto)
    {
        try
        {
            var customer =
                await _unitOfWork.GenaricRepository<Customer>()
                    .GetByIdAsync(id);

            if (customer == null)
                return 0;

            customer.CustomerName = dto.Name;
            customer.Phone = dto.Phone;
            customer.Address = dto.Address;
            customer.Email = dto.Email;

            _unitOfWork.GenaricRepository<Customer>()
                .Update(customer);

            await _unitOfWork.SaveChangesAsync();

            return 1;
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while updating customer.",
                ex);
        }
    }

    public async Task<int> DeleteAsync(int id)
    {
        try
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
        catch (Exception ex)
        {
            throw new Exception(
                "Error while deleting customer.",
                ex);
        }
    }
}