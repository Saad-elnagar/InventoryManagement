using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL;
using InventoryManagementSystem.DAL.Entities;
using InventoryManagementSystem.DAL.Repository;

namespace InventoryManagementSystem.BLL.Service;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)

    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardDTO> GetDashboardAsync()
    {
        try
        {
            var totalProducts =
                await _unitOfWork.GenaricRepository<Product>().CountAsync();

            var totalCategories =
                await _unitOfWork.GenaricRepository<Category>().CountAsync();

            var totalSuppliers =
                await _unitOfWork.GenaricRepository<Supplier>().CountAsync();

            var totalCustomers =
                await _unitOfWork.GenaricRepository<Customer>().CountAsync();

            var lowStockProducts =
                await _unitOfWork.GenaricRepository<Product>().CountAsync(p => p.StockQuantity <= p.LowStockThreshold);

            var totalPurchases =
                await _unitOfWork.GenaricRepository<Purchase>().SumAsync(p => p.TotalAmount);

            var totalSales =
                await _unitOfWork.GenaricRepository<Sale>().SumAsync(s => s.TotalAmount);

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