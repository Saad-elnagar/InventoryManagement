using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class InventoryAiController : Controller
{
    private readonly IInventoryAiAssistant _assistant;

    public InventoryAiController(
        IInventoryAiAssistant assistant)
    {
        _assistant = assistant;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(
        [FromBody] List<InventoryAiMessageDTO> messages,
        CancellationToken cancellationToken)
    {
        if (messages == null || messages.Count == 0)
        {
            return BadRequest(
                new { message = "Please enter a question." });
        }

        try
        {
            var answer =
                await _assistant.ChatAsync(
                    messages
                        .TakeLast(20)
                        .ToList(),
                    cancellationToken);

            return Json(new { answer });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Inventory AI could not process this request." });
        }
    }
}
