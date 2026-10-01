using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int page = 1,
        int pageSize = 10)
    {
        ViewBag.Search = search;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var products = await _productService.SearchAsync(search,page,pageSize);

            return View(products);
        }

        var result =
            await _productService.GetPagedAsync(page, pageSize);

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductDTO dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            var product = await _productService.CreateAsync(dto);

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductDTO dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            var result = await _productService.UpdateAsync(id, dto);

            if (!result)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var result = await _productService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> LowStock()
    {
        var products = await _productService.GetLowStockAsync();

        return View(products);
    }
} 