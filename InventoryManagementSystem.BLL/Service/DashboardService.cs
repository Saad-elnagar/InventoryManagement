using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

namespace InventoryManagementSystem.BLL.Service;

public class DashboardService : IDashboardService
{
    private readonly IGenaricRepository<Product> _productRepository;
    private readonly IGenaricRepository<Category> _categoryRepository;
    private readonly IGenaricRepository<Supplier> _supplierRepository;
    private readonly IGenaricRepository<Customer> _customerRepository;
    private readonly IGenaricRepository<Purchase> _purchaseRepository;
    private readonly IGenaricRepository<Sale> _saleRepository;

    public DashboardService(
        IGenaricRepository<Product> productRepository,
        IGenaricRepository<Category> categoryRepository,
        IGenaricRepository<Supplier> supplierRepository,
        IGenaricRepository<Customer> customerRepository,
        IGenaricRepository<Purchase> purchaseRepository,
        IGenaricRepository<Sale> saleRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _supplierRepository = supplierRepository;
        _customerRepository = customerRepository;
        _purchaseRepository = purchaseRepository;
        _saleRepository = saleRepository;
    }

    public async Task<DashboardDTO> GetDashboardAsync()
    {
        try
        {
            var totalProducts =
                await _productRepository.CountAsync();

            var totalCategories =
                await _categoryRepository.CountAsync();

            var totalSuppliers =
                await _supplierRepository.CountAsync();

            var totalCustomers =
                await _customerRepository.CountAsync();

            var lowStockProducts =
                await _productRepository.CountAsync(
                    p => p.StockQuantity <= p.LowStockThreshold);

            var totalPurchases =
                await _purchaseRepository.SumAsync(
                    p => p.TotalAmount);

            var totalSales =
                await _saleRepository.SumAsync(
                    s => s.TotalAmount);

            return new DashboardDTO
            {
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                TotalSuppliers = totalSuppliers,
                TotalCustomers = totalCustomers,
                LowStockProducts = lowStockProducts,
                TotalPurchases = totalPurchases,
                TotalSales = totalSales
            };
        }
        catch (Exception ex)
        {
            throw new Exception(
                "Error while loading dashboard.",
                ex);
        }
    }
}