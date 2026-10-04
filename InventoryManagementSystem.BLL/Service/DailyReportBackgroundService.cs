using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.BLL.Service;

public class DailyReportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyReportBackgroundService> _logger;

    public DailyReportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DailyReportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendDailyReportAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build/send the daily report.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    public async Task SendDailyReportAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        var aiSummarizer = scope.ServiceProvider.GetRequiredService<IAiReportSummarizer>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var recipient = configuration["Report:RecipientEmail"];

        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Report:RecipientEmail is not configured. Skipping send.");
            return;
        }

        var report = await reportService.BuildDailyReportAsync();
        var summary = await aiSummarizer.SummarizeAsync(report);

        await emailService.SendEmailAsync(
            recipient,
            $"Daily Inventory Report - {report.To:yyyy-MM-dd}",
            summary);

        _logger.LogInformation("Daily report sent to {Recipient}.", recipient);
    }
}