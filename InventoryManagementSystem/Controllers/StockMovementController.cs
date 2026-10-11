using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

[Authorize]
public class StockMovementController : Controller
{
    private readonly IStockMovementService _stockMovementService;
    private readonly IProductService _productService;

    public StockMovementController(
        IStockMovementService stockMovementService,
        IProductService productService)
    {
        _stockMovementService = stockMovementService;
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? productId,
        string? movementType,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var movements =
            await _stockMovementService.GetAllAsync(
                productId,
                movementType,
                fromDate,
                toDate);

        var products =
            await _productService.GetAllAsync();

        ViewBag.Products = products;

        ViewBag.ProductId = productId;

        ViewBag.MovementType = movementType;

        ViewBag.FromDate =
            fromDate?.ToString("yyyy-MM-dd");

        ViewBag.ToDate =
            toDate?.ToString("yyyy-MM-dd");

        ViewBag.MovementTypes =
            Enum.GetValues<StockMovementType>();

        return View(movements);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var movement =
            await _stockMovementService.GetByIdAsync(id);

        if (movement == null)
            return NotFound();

        return View(movement);
    }
}