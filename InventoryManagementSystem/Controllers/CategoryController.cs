using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllAsync();
            return View(categories);
        }

        // GET: Category/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
      
        public async Task<IActionResult> Create(CategoryDTO categoryDto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _categoryService.CreateAsync(categoryDto);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // إظهار رسالة الخطأ للمستخدم فوق الفورم مباشرة دون أن تتوقف الصفحة
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(categoryDto);
        }
    }
}