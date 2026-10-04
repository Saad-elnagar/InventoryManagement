using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL;
using InventoryManagementSystem.DAL.Entities;

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
        var productRepository =
            _unitOfWork.GenaricRepository<Product>();

        var categoryRepository =
            _unitOfWork.GenaricRepository<Category>();

        var supplierRepository =
            _unitOfWork.GenaricRepository<Supplier>();

        var customerRepository =
            _unitOfWork.GenaricRepository<Customer>();

        var purchaseRepository =
            _unitOfWork.GenaricRepository<Purchase>();

        var saleRepository =
            _unitOfWork.GenaricRepository<Sale>();

        var stockMovementRepository =
            _unitOfWork.GenaricRepository<StockMovement>();

        var products =
            (await productRepository.GetAllAsync()).ToList();

        var totalCategories =
            await categoryRepository.CountAsync();

        var totalSuppliers =
            await supplierRepository.CountAsync();

        var totalCustomers =
            await customerRepository.CountAsync();

        var totalPurchases =
            await purchaseRepository.SumAsync(
                x => x.TotalAmount);

        var totalSales =
            await saleRepository.SumAsync(
                x => x.TotalAmount);

        var totalStock =
            products.Sum(x => x.StockQuantity);

        var lowStockProducts =
            products.Count(x =>
                x.StockQuantity > 0 &&
                x.StockQuantity <= x.LowStockThreshold);

        var outOfStockProducts =
            products.Count(x =>
                x.StockQuantity == 0);

        var inventoryValue =
            products.Sum(x =>
                x.UnitPrice * x.StockQuantity);

        var categoryIds =
            products
                .Select(x => x.CategoryId)
                .Distinct()
                .ToList();

        var categories = categoryIds.Count > 0
            ? (await categoryRepository.GetWhereAsync(
                x => categoryIds.Contains(x.Id))).ToList()
            : [];

        var categoryDictionary =
            categories.ToDictionary(
                x => x.Id,
                x => x.Name);

        var categoryStatistics =
            products
                .GroupBy(x => x.CategoryId)
                .Select(group => new DashboardCategoryDTO
                {
                    CategoryId = group.Key,

                    CategoryName =
                        categoryDictionary.TryGetValue(
                            group.Key,
                            out var categoryName)
                            ? categoryName
                            : $"Category {group.Key}",

                    ProductCount =
                        group.Count()
                })
                .OrderByDescending(x => x.ProductCount)
                .ToList();

        var currentYear = DateTime.Now.Year;

        var startDate =
            new DateTime(currentYear, 1, 1);

        var endDate =
            startDate.AddYears(1);

        var movements =
            (await stockMovementRepository.GetWhereAsync(
                x => x.MovementDate >= startDate &&
                     x.MovementDate < endDate))
            .ToList();

        var stockMovements =
            Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var monthMovements =
                        movements.Where(x =>
                            x.MovementDate.Month == month);

                    return new DashboardStockMovementDTO
                    {
                        Month =
                            new DateTime(
                                currentYear,
                                month,
                                1).ToString("MMM"),

                        Inbound =
                            monthMovements
                                .Where(x => x.Quantity > 0)
                                .Sum(x => x.Quantity),

                        Outbound =
                            monthMovements
                                .Where(x => x.Quantity < 0)
                                .Sum(x => Math.Abs(x.Quantity))
                    };
                })
                .ToList();

        var dashboardProducts =
            products
                .OrderBy(x => x.StockQuantity)
                .ThenBy(x => x.Name)
                .Take(5)
                .Select(x => new DashboardProductDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Quantity = x.StockQuantity,
                    ReorderLevel = x.LowStockThreshold,

                    Status =
                        x.StockQuantity == 0
                            ? "Out Of Stock"
                            : x.StockQuantity <= x.LowStockThreshold
                                ? "Low"
                                : "Available"
                })
                .ToList();

        return new DashboardDTO
        {
            TotalProducts = products.Count,
            TotalCategories = totalCategories,
            TotalSuppliers = totalSuppliers,
            TotalCustomers = totalCustomers,

            TotalStock = totalStock,
            LowStockProducts = lowStockProducts,
            OutOfStockProducts = outOfStockProducts,

            InventoryValue = inventoryValue,

            TotalPurchases = totalPurchases,
            TotalSales = totalSales,

            StockMovements = stockMovements,
            CategoryStatistics = categoryStatistics,
            Products = dashboardProducts
        };
    }
}