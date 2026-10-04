using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoryController(
        ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories =
            await _categoryService.GetAllAsync();

        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CategoryDTO categoryDto)
    {
        if (!ModelState.IsValid)
            return View(categoryDto);

        try
        {
            await _categoryService.CreateAsync(categoryDto);

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(categoryDto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var category =
            await _categoryService.GetByIdAsync(id);

        if (category == null)
            return NotFound();

        return View(category);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category =
            await _categoryService.GetByIdAsync(id);

        if (category == null)
            return NotFound();

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CategoryDTO categoryDto)
    {
        if (!ModelState.IsValid)
            return View(categoryDto);

        try
        {
            var result =
                await _categoryService.UpdateAsync(
                    id,
                    categoryDto);

            if (!result)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(categoryDto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var category =
            await _categoryService.GetByIdAsync(id);

        if (category == null)
            return NotFound();

        return View(category);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var result =
                await _categoryService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            var category =
                await _categoryService.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            return View("Delete", category);
        }
    }
}