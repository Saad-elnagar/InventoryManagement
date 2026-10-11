using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Service;
using Moq;

namespace InventoryManagementSystem.Tests;

public class ReportServiceTests
{
    [Fact]
    public async Task BuildDailyReportAsync_CountsMovementsAndLowStockProducts()
    {
        var stockMovementService = new Mock<IStockMovementService>();
        var productService = new Mock<IProductService>();

        stockMovementService
            .Setup(s => s.GetAllAsync(
                null,
                null,
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(new[]
            {
                new StockMovementDTO
                {
                    ProductId = 1,
                    MovementType = StockMovementType.Sale.ToString(),
                    Quantity = -2,
                    MovementDate = DateTime.Now
                },
                new StockMovementDTO
                {
                    ProductId = 2,
                    MovementType = StockMovementType.Purchase.ToString(),
                    Quantity = 5,
                    MovementDate = DateTime.Now
                },
                new StockMovementDTO
                {
                    ProductId = 1,
                    MovementType = StockMovementType.Sale.ToString(),
                    Quantity = -1,
                    MovementDate = DateTime.Now
                }
            });

        productService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(new[]
            {
                new ProductDTO
                {
                    Id = 1,
                    Name = "Mouse",
                    Quantity = 2,
                    ReorderLevel = 3
                },
                new ProductDTO
                {
                    Id = 2,
                    Name = "Monitor",
                    Quantity = 20,
                    ReorderLevel = 3
                }
            });

        var report = await new ReportService(
            stockMovementService.Object,
            productService.Object)
            .BuildDailyReportAsync();

        Assert.Equal(2, report.TotalProducts);
        Assert.Equal(3, report.MovementsCount);
        Assert.Single(report.LowStockProducts);
        Assert.Equal(1, report.LowStockProducts[0].ProductId);

        var sales = report.MovementsByType
            .Single(x => x.MovementType == StockMovementType.Sale.ToString());

        Assert.Equal(2, sales.Count);
        Assert.Equal(-3, sales.TotalQuantity);
    }
}
