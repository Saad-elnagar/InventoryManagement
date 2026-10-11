using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager,Employee")]
public class SaleController : Controller
{
    private readonly ISaleService _saleService;
    private readonly IProductService _productService;
    private readonly ICustomerService _customerService;

    public SaleController(
        ISaleService saleService,
        IProductService productService,
        ICustomerService customerService)
    {
        _saleService = saleService;
        _productService = productService;
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sales =
            await _saleService.GetAllAsync();

        return View(sales);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var sale =
            await _saleService.GetByIdAsync(id);

        if (sale == null)
            return NotFound();

        return View(sale);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCreateData();

        return View(
            new SaleDTO
            {
                SaleDate = DateTime.Now
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SaleDTO saleDto)
    {
        if (!ModelState.IsValid)
        {
            await LoadCreateData();
            return View(saleDto);
        }

        try
        {
            await _saleService.CreateAsync(saleDto);

            return RedirectToAction(
                nameof(Details),
                new { id = saleDto.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            await LoadCreateData();

            return View(saleDto);
        }
    }

    private async Task LoadCreateData()
    {
        ViewBag.Products =
            await _productService.GetAllAsync();

        ViewBag.Customers =
            await _customerService.GetAllAsync();
    }
}
