using InventoryManagementSystem.BLL.DTOs;
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

        ViewBag.Products =
            await _productService.GetAllAsync();

        ViewBag.ProductId = productId;
        ViewBag.MovementType = movementType;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.MovementTypes =
            Enum.GetValues<StockMovementType>();

        return View(movements);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCreateData();

        return View(new CreateStockMovementDTO());
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateStockMovementDTO dto)
    {
        var manualTypes =
            new[]
            {
                StockMovementType.Damage,
                StockMovementType.Lost,
                StockMovementType.Found,
                StockMovementType.Adjustment,
                StockMovementType.PurchaseReturn,
                StockMovementType.SaleReturn,
                StockMovementType.OpeningStock,
                StockMovementType.VendorGift
            };

        if (!dto.MovementType.HasValue ||
            !manualTypes.Contains(dto.MovementType.Value))
        {
            ModelState.AddModelError(
                nameof(dto.MovementType),
                "Please select a valid manual movement type.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCreateData();
            return View(dto);
        }

        try
        {
            await _stockMovementService.CreateAsync(dto);

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            await LoadCreateData();

            return View(dto);
        }
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

    private async Task LoadCreateData()
    {
        ViewBag.Products =
            await _productService.GetAllAsync();

        ViewBag.MovementTypes =
            new[]
            {
                StockMovementType.Damage,
                StockMovementType.Lost,
                StockMovementType.Found,
                StockMovementType.Adjustment,
                StockMovementType.PurchaseReturn,
                StockMovementType.SaleReturn,
                StockMovementType.OpeningStock,
                StockMovementType.VendorGift
            };
    }
}
