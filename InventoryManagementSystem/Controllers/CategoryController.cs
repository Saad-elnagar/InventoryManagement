using InventoryManagementSystem.BLL.Service; 
using Microsoft.AspNetCore.Mvc;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.DTOs;


namespace InventoryManagementSystem.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllAsync();
            return View(categories);
        }
    }
}