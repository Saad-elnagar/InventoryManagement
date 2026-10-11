
using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager,Employee")]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;

    public CustomerController(
        ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customers =
            await _customerService.GetAllAsync();

        return View(customers);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CustomerDTO());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CustomerDTO dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            await _customerService.CreateAsync(dto);

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
        var customer =
            await _customerService.GetByIdAsync(id);

        if (customer == null)
            return NotFound();

        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer =
            await _customerService.GetByIdAsync(id);

        if (customer == null)
            return NotFound();

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        CustomerDTO dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        try
        {
            int result =
                await _customerService.UpdateAsync(
                    id,
                    dto);

            if (result == 0)
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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer =
            await _customerService.GetByIdAsync(id);

        if (customer == null)
            return NotFound();

        return View(customer);
    }

    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = "Admin,Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            int result =
                await _customerService.DeleteAsync(id);

            if (result == 0)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            var customer =
                await _customerService.GetByIdAsync(id);

            if (customer == null)
                return NotFound();

            ModelState.AddModelError(
                string.Empty,
                ex.Message);

            return View("Delete", customer);
        }
    }
}