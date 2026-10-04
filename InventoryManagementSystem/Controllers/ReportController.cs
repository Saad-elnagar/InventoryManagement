using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.BLL.Service;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Controllers;

public class ReportController : Controller
{
    private readonly IReportService _reportService;
    private readonly IAiReportSummarizer _aiReportSummarizer;
    private readonly DailyReportBackgroundService _backgroundService;

    public ReportController(
        IReportService reportService,
        IAiReportSummarizer aiReportSummarizer,
        IEnumerable<Microsoft.Extensions.Hosting.IHostedService> hostedServices)
    {
        _reportService = reportService;
        _aiReportSummarizer = aiReportSummarizer;
        _backgroundService = hostedServices.OfType<DailyReportBackgroundService>().First();
    }

    [HttpGet]
    public async Task<IActionResult> Daily()
    {
        var report = await _reportService.BuildDailyReportAsync();
        return Json(report);
    }

    [HttpGet]
    public async Task<IActionResult> Preview()
    {
        var report = await _reportService.BuildDailyReportAsync();
        var summary = await _aiReportSummarizer.SummarizeAsync(report);
        return Json(new { report, summary });
    }

    [HttpPost]
    public async Task<IActionResult> SendNow()
    {
        await _backgroundService.SendDailyReportAsync();
        return Ok("Report sent.");
    }
}