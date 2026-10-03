using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Enums;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

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

    public async Task<IActionResult> Index(
        int? productId,
        StockMovementType? movementType)
    {
        IEnumerable<StockMovementDTO> movements =
            productId.HasValue
                ? await _stockMovementService.GetByProductIdAsync(productId.Value)
                : await _stockMovementService.GetAllAsync();

        if (movementType.HasValue)
        {
            movements = movements.Where(m => m.MovementType == movementType.Value);
        }

        ViewBag.Products =
            await _productService.GetAllAsync();

        ViewBag.SelectedProductId = productId;
        ViewBag.SelectedMovementType = movementType;

        return View(
            movements
                .OrderByDescending(m => m.MovementDate)
                .ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var movement =
            await _stockMovementService.GetByIdAsync(id);

        if (movement == null)
            return NotFound();

        return View(movement);
    }
}