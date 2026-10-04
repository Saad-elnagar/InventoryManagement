using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Service;
using InventoryManagementSystem.Models.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace InventoryManagementSystem.Controllers;

[Authorize(Roles = "Admin")]
public class ReportController : Controller
{
    private readonly IReportService _reportService;
    private readonly IAiReportSummarizer _aiReportSummarizer;
    private readonly DailyReportBackgroundService _backgroundService;

    public ReportController(
        IReportService reportService,
        IAiReportSummarizer aiReportSummarizer,
        IEnumerable<IHostedService> hostedServices)
    {
        _reportService = reportService;
        _aiReportSummarizer = aiReportSummarizer;

        _backgroundService =
            hostedServices
                .OfType<DailyReportBackgroundService>()
                .First();
    }

    [HttpGet]
    public async Task<IActionResult> Daily()
    {
        var report =
            await _reportService
                .BuildDailyReportAsync();

        return Json(report);
    }

    [HttpGet]
    public async Task<IActionResult> Preview()
    {
        var report =
            await _reportService
                .BuildDailyReportAsync();

        var summary =
            await _aiReportSummarizer
                .SummarizeAsync(report);

        var model =
            new DailyReportPreviewViewModel
            {
                Report = report,
                Summary = summary
            };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNow()
    {
        try
        {
            await _backgroundService
                .SendDailyReportAsync();

            TempData["Success"] =
                "Daily report sent successfully.";

            return RedirectToAction(
                nameof(Preview));
        }
        catch (Exception ex)
        {
            TempData["Error"] =
                ex.Message;

            return RedirectToAction(
                nameof(Preview));
        }
    }
}