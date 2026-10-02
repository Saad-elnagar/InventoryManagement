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
        IEnumerable<StockMovementDTO> movements;

        if (productId.HasValue)
        {
            movements =
                await _stockMovementService.GetByProductIdAsync(
                    productId.Value);
        }
        else if (movementType.HasValue)
        {
            movements =
                await _stockMovementService.GetByTypeAsync(
                    movementType.Value);
        }
        else
        {
            movements =
                await _stockMovementService.GetAllAsync();
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