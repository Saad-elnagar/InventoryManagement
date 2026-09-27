using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

namespace InventoryManagementSystem.BLL.Service;

public class SupplierService   : ISupplierService
{
    private readonly IGenaricRepository<Supplier> _supplierRepository;
    private readonly IGenaricRepository<Purchase> _purchaseRepository;

    public SupplierService(
        IGenaricRepository<Supplier> supplierRepository,
        IGenaricRepository<Purchase> purchaseRepository)
    {
        _supplierRepository = supplierRepository;
        _purchaseRepository = purchaseRepository;
    }
    public async Task<IEnumerable<SupplierDTO>> GetAllAsync()
    {
       var supplier=   await _supplierRepository.GetAllAsync ();
         return supplier
             .Select(s => new SupplierDTO
             {
                 Id = s.Id,
                 SupplierName = s.SupplierName,
                 Phone = s.Phone,
                 Email = s.Email,
                 Address = s.Address
             });
       
    }

    public async Task<SupplierDTO?> GetByIdAsync(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);

        if (supplier == null)
            throw new Exception("Supplier not exists");

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
        var excit = _supplierRepository.GetByIdAsync(dto.Id);
        if (excit != null)
        {
            throw new Exception("Supplier already exists");
        }
        var supplier= new Supplier(){
        SupplierName = dto.SupplierName,
        ContactName = dto.ContactName,
        Phone = dto.Phone,
        Email = dto.Email,
        Address = dto.Address
    };

    await _supplierRepository.AddAsync(supplier);
  //  await _supplierRepository.SaveChangesAsync();
    dto.Id = supplier.Id;
    return dto;

    }

    public async Task<bool> UpdateAsync(int id, SupplierDTO dto)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);

        if (supplier is null)
            return false;

        var exists = await _supplierRepository.AnyAsync(
            s => s.Id != id &&
                 s.SupplierName == dto.SupplierName);

        if (exists)
            throw new Exception("Supplier already exists.");

        supplier.SupplierName = dto.SupplierName;
        supplier.ContactName = dto.ContactName;
        supplier.Phone = dto.Phone;
        supplier.Email = dto.Email;
        supplier.Address = dto.Address;

        _supplierRepository.Update(supplier);

        // await _supplierRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);

        if (supplier == null)
            return false;

        var hasPurchases = await _purchaseRepository.AnyAsync(
            p => p.SupplierId == id);

        if (hasPurchases)
            throw new Exception(
                "Cannot delete supplier because it has purchase history.");

        _supplierRepository.Delete(supplier);

     //   await _supplierRepository.SaveChangesAsync();

        return true;
    }
}