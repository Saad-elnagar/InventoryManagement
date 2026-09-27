using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using InventoryManagementSystem.BLL.Interfaces; 
using InventoryManagementSystem.BLL.DTOs;
namespace InventoryManagementSystem.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllAsync();
            return View(products);
        }

    }
}
