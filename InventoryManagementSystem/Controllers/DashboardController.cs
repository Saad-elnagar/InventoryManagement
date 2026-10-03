using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Web.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;

        public DashboardController(
            IDashboardService dashboardService,
            ICategoryService categoryService,
            IProductService productService)
        {
            _dashboardService = dashboardService;
            _categoryService = categoryService;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = await _dashboardService.GetDashboardAsync();

           
            var categories = await _categoryService.GetAllAsync();
            var products = await _productService.GetAllAsync();

            ViewBag.TotalCategories = categories?.Count() ?? 0;
            ViewBag.TotalProducts = products?.Count() ?? 0;

            return View(dashboard);
        }
    }
}