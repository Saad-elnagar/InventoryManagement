using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;

namespace InventoryManagementSystem.BLL.Service;

public class ReportService : IReportService
{
    private readonly IStockMovementService _stockMovementService;
    private readonly IProductService _productService;

    public ReportService(
        IStockMovementService stockMovementService,
        IProductService productService)
    {
        _stockMovementService = stockMovementService;
        _productService = productService;
    }

    public async Task<DailyReportDTO> BuildDailyReportAsync()
    {
        var to = DateTime.Now;
        var from = to.AddHours(-24);

        // Same parameter order used by StockMovementController:
        // productId, movementType, fromDate, toDate
        var movements =
            (await _stockMovementService.GetAllAsync(null, null, from, to))
            .ToList();

        var products =
            (await _productService.GetAllAsync())
            .ToList();

        return new DailyReportDTO
        {
            From = from,
            To = to,
            TotalProducts = products.Count,
            MovementsCount = movements.Count,

            MovementsByType = movements
                .GroupBy(m => m.MovementType)
                .Select(g => new MovementTypeSummaryDTO
                {
                    MovementType = g.Key,
                    Count = g.Count(),
                    TotalQuantity = g.Sum(m => m.Quantity)
                })
                .OrderByDescending(x => x.Count)
                .ToList(),

            LowStockProducts = products
                .Where(p => p.Quantity <= p.ReorderLevel)
                .OrderBy(p => p.Quantity)
                .Select(p => new LowStockItemDTO
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Quantity = p.Quantity,
                    ReorderLevel = p.ReorderLevel
                })
                .ToList()
        };
    }
}