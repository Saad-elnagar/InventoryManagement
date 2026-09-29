using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Service;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<SupplierDTO>> GetAllAsync()
    {
        var suppliers =
            await _unitOfWork.GenaricRepository<Supplier>()
                .GetAllAsync();

        return suppliers.Select(s => new SupplierDTO
        {
            Id = s.Id,
            SupplierName = s.SupplierName,
            ContactName = s.ContactName,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address
        });
    }

    public async Task<SupplierDTO?> GetByIdAsync(int id)
    {
        var supplier =
            await _unitOfWork.GenaricRepository<Supplier>()
                .GetByIdAsync(id);

        if (supplier == null)
            return null;

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

    public async Task<SupplierDTO> CreateAsync(SupplierDTO dto)
    {
        var exists =
            await _unitOfWork.GenaricRepository<Supplier>()
                .AnyAsync(s => s.SupplierName == dto.SupplierName);

        if (exists)
            throw new Exception("Supplier already exists.");

        var supplier = new Supplier
        {
            SupplierName = dto.SupplierName,
            ContactName = dto.ContactName,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address
        };

        await _unitOfWork.GenaricRepository<Supplier>()
            .AddAsync(supplier);

        await _unitOfWork.SaveChangesAsync();

        dto.Id = supplier.Id;

        return dto;
    }

    public async Task<bool> UpdateAsync(int id, SupplierDTO dto)
    {
        var supplier =
            await _unitOfWork.GenaricRepository<Supplier>()
                .GetByIdAsync(id);

        if (supplier == null)
            return false;

        var exists =
            await _unitOfWork.GenaricRepository<Supplier>()
                .AnyAsync(s =>
                    s.Id != id &&
                    s.SupplierName == dto.SupplierName);

        if (exists)
            throw new Exception("Supplier already exists.");

        supplier.SupplierName = dto.SupplierName;
        supplier.ContactName = dto.ContactName;
        supplier.Phone = dto.Phone;
        supplier.Email = dto.Email;
        supplier.Address = dto.Address;

        _unitOfWork.GenaricRepository<Supplier>()
            .Update(supplier);

        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier =
            await _unitOfWork.GenaricRepository<Supplier>()
                .GetByIdAsync(id);

        if (supplier == null)
            return false;

        var hasPurchases =
            await _unitOfWork.GenaricRepository<Purchase>()
                .AnyAsync(p => p.SupplierId == id);

        if (hasPurchases)
            throw new Exception(
                "Cannot delete supplier because it has purchase history.");

        _unitOfWork.GenaricRepository<Supplier>()
            .Delete(supplier);

        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}