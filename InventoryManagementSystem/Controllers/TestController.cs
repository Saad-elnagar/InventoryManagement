using InventoryManagementSystem.DAL.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Web.Controllers;

public class TestController : Controller
{
  //  private readonly ILogger<TestController> _logger;
    private readonly ApplicationDbContext _context;
    
    public  TestController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var all = _context.Products.ToList();
        
        return View(all);
        

    }

}