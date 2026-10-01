using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public ProductController(
        IProductService productService,
        ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int page = 1,
        int pageSize = 10)
    {
        ViewBag.Search = search;

        // ViewBag.Statistics =
        //     await _productService.GetStatisticsAsync();

        PaginationResult<ProductDTO> result;

        if (!string.IsNullOrWhiteSpace(search))
        {
            result = await _productService.SearchAsync(
                search,
                page,
                pageSize);
        }
        else
        {
            result = await _productService.GetPagedAsync(
                page,
                pageSize);
        }

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product =
            await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories =
            await _categoryService.GetAllAsync();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductDTO dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories =
                await _categoryService.GetAllAsync();

            return View(dto);
        }

        try
        {
            await _productService.CreateAsync(dto);

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);

            ViewBag.Categories =
                await _categoryService.GetAllAsync();

            return View(dto);
        }
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product =
            await _productService.GetByIdAsync(id);

        if (product == null)
            return NotFound();

        ViewBag.Categories =
            await _categoryService.GetAllAsync();

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        ProductDTO dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories =
                await _categoryService.GetAllAsync();

            return View(dto);
        }

        try
        {
            var result =
                await _productService.UpdateAsync(
                    id,
                    dto);

            if (!result)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                "",
                ex.Message);

            ViewBag.Categories =
                await _categoryService.GetAllAsync();

            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product =
            await _productService.GetByIdAsync(id);

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
            var result =
                await _productService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                "",
                ex.Message);

            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> LowStock()
    {
        var products =
            await _productService.GetLowStockAsync();

        return View(products);
    }

    // [HttpGet]
    // public async Task<IActionResult> Suggestions(
    //     string term)
    // {
    //     var result =
    //         await _productService.GetSuggestionsAsync(term);
    //
    //     return Json(result);
    // }
}