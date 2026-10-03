using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers
{
    public class SaleController : Controller
    {
        private readonly ISaleService _saleService;

        public SaleController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        // GET: Sale
        public async Task<IActionResult> Index()
        {
            var sales = await _saleService.GetAllAsync();
            return View(sales);
        }

        // GET: Sale/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sale/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaleDTO saleDto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _saleService.CreateAsync(saleDto);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(saleDto);
        }
    }
}