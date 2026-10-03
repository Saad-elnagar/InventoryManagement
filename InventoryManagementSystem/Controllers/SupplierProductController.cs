using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers
{
    public class SupplierProductController : Controller
    {
        private readonly ISupplierService _supplierService;

        public SupplierProductController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        public async Task<IActionResult> Index()
        {
            var suppliers = await _supplierService.GetAllAsync();
            return View(suppliers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _supplierService.CreateAsync(dto);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SupplierDTO dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var updated = await _supplierService.UpdateAsync(id, dto);

            if (!updated)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);

            if (supplier == null)
                return NotFound();

            return View(supplier);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleted = await _supplierService.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }
    }
}