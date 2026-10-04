using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class SupplierController : Controller
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var suppliers = await _supplierService.GetAllAsync();

        return View(suppliers);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new SupplierDTO());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierDTO dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            await _supplierService.CreateAsync(dto);

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var supplier =
            await _supplierService.GetByIdAsync(id);

        if (supplier == null)
            return NotFound();

        return View(supplier);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier =
            await _supplierService.GetByIdAsync(id);

        if (supplier == null)
            return NotFound();

        return View(supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        SupplierDTO dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            var result =
                await _supplierService.UpdateAsync(
                    id,
                    dto);

            if (!result)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View(dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier =
            await _supplierService.GetByIdAsync(id);

        if (supplier == null)
            return NotFound();

        return View(supplier);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var result =
                await _supplierService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            var supplier =
                await _supplierService.GetByIdAsync(id);

            if (supplier == null)
                return NotFound();

            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View("Delete", supplier);
        }
    }
}