using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class PurchaseController : Controller
{
    private readonly IPurchaseService _purchaseService;
    private readonly ISupplierService _supplierService;
    private readonly IProductService _productService;

    public PurchaseController(
        IPurchaseService purchaseService,
        ISupplierService supplierService,
        IProductService productService)
    {
        _purchaseService = purchaseService;
        _supplierService = supplierService;
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var purchases =
            await _purchaseService.GetAllAsync();

        return View(purchases);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var purchase =
            await _purchaseService.GetByIdAsync(id);

        if (purchase == null)
            return NotFound();

        return View(purchase);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCreateData();

        return View(
            new PurchaseDTO
            {
                PurchaseDate = DateTime.Now
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        PurchaseDTO dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateData();

            return View(dto);
        }

        try
        {
            var purchase =
                await _purchaseService.CreateAsync(dto);

            return RedirectToAction(
                nameof(Details),
                new { id = purchase.Id });
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

    private async Task LoadCreateData()
    {
        ViewBag.Suppliers =
            await _supplierService.GetAllAsync();

        ViewBag.Products =
            await _productService.GetAllAsync();
    }
}