using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.BLL.Service;

public class DailyReportBackgroundService
    : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyReportBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public DailyReportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DailyReportBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sendHour =
                    _configuration.GetValue(
                        "Report:SendHour",
                        23);

                var sendMinute =
                    _configuration.GetValue(
                        "Report:SendMinute",
                        0);

                var now = DateTime.Now;

                var nextRun =
                    now.Date
                        .AddHours(sendHour)
                        .AddMinutes(sendMinute);

                if (nextRun <= now)
                {
                    nextRun =
                        nextRun.AddDays(1);
                }

                var delay =
                    nextRun - now;

                _logger.LogInformation(
                    "Next daily report scheduled for {NextRun}.",
                    nextRun);

                await Task.Delay(
                    delay,
                    stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await SendDailyReportAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to build/send the daily report.");

                await Task.Delay(
                    TimeSpan.FromMinutes(5),
                    stoppingToken);
            }
        }
    }

    public async Task<bool> SendDailyReportAsync(
        bool failIfNotConfigured = false)
    {
        using var scope =
            _serviceProvider.CreateScope();

        var reportService =
            scope.ServiceProvider
                .GetRequiredService<IReportService>();

        var aiSummarizer =
            scope.ServiceProvider
                .GetRequiredService<IAiReportSummarizer>();

        var emailService =
            scope.ServiceProvider
                .GetRequiredService<IEmailService>();

        var recipient =
            _configuration[
                "Report:RecipientEmail"];

        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning(
                "Report:RecipientEmail is not configured.");

            if (failIfNotConfigured)
            {
                throw new InvalidOperationException(
                    "Report recipient email is not configured.");
            }

            return false;
        }

        var report =
            await reportService
                .BuildDailyReportAsync();

        var summary =
            await aiSummarizer
                .SummarizeAsync(report);

        await emailService.SendEmailAsync(
            recipient,
            $"Daily Inventory Report - {report.To:yyyy-MM-dd}",
            summary);

        _logger.LogInformation(
            "Daily report sent to {Recipient}.",
            recipient);

        return true;
    }
}