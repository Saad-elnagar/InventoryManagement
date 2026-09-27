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
            var products =
                await _productRepository.GetAllAsync();

            var categories =
                await _categoryRepository.GetAllAsync();

            var suppliers =
                await _supplierRepository.GetAllAsync();

            var customers =
                await _customerRepository.GetAllAsync();

            var purchases =
                await _purchaseRepository.GetAllAsync();

            var sales =
                await _saleRepository.GetAllAsync();

            return new DashboardDTO
            {
                TotalProducts = products.Count(),
                TotalCategories = categories.Count(),
                TotalSuppliers = suppliers.Count(),
                TotalCustomers = customers.Count(),

                LowStockProducts = products.Count(
                    p => p.StockQuantity <= p.LowStockThreshold),

                TotalPurchases = purchases.Sum(
                    p => p.TotalAmount),

                TotalSales = sales.Sum(
                    s => s.TotalAmount)
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