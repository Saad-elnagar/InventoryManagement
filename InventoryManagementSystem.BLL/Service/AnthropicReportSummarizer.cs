using System.Text;
using System.Text.Json;
using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.BLL.Service;

public class AnthropicReportSummarizer : IAiReportSummarizer
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const string Model = "claude-sonnet-4-5";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnthropicReportSummarizer> _logger;

    public AnthropicReportSummarizer(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AnthropicReportSummarizer> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> SummarizeAsync(DailyReportDTO report)
    {
        var apiKey = _configuration["Anthropic:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning(
                "Anthropic:ApiKey is not configured. Sending a plain-text report instead.");

            return BuildFallbackSummary(report);
        }

        var prompt = BuildPrompt(report);

        var requestBody = new
        {
            model = Model,
            max_tokens = 600,
            messages = new[] { new { role = "user", content = prompt } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Anthropic API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                return BuildFallbackSummary(report);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();

            return string.IsNullOrWhiteSpace(text) ? BuildFallbackSummary(report) : text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI summary, falling back to plain text.");
            return BuildFallbackSummary(report);
        }
    }

    private static string BuildPrompt(DailyReportDTO report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an inventory management assistant. Write a short, clear daily report email in Arabic for a warehouse manager, based on this data:");
        sb.AppendLine();
        sb.AppendLine($"Period: {report.From:yyyy-MM-dd HH:mm} to {report.To:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Total products: {report.TotalProducts}");
        sb.AppendLine($"Total stock movements: {report.MovementsCount}");
        sb.AppendLine();
        sb.AppendLine("Movements by type:");

        if (!report.MovementsByType.Any())
            sb.AppendLine("- None");
        else
            foreach (var m in report.MovementsByType)
                sb.AppendLine($"- {m.MovementType}: {m.Count} movements, {m.TotalQuantity} units");

        sb.AppendLine();
        sb.AppendLine("Low stock products (quantity <= reorder level):");

        if (!report.LowStockProducts.Any())
            sb.AppendLine("- None");
        else
            foreach (var p in report.LowStockProducts)
                sb.AppendLine($"- {p.Name}: quantity {p.Quantity}, reorder level {p.ReorderLevel}");

        sb.AppendLine();
        sb.AppendLine("Keep it concise (under 200 words), friendly but professional, and clearly highlight anything urgent such as low stock items. Write the whole email in Arabic.");

        return sb.ToString();
    }

    private static string BuildFallbackSummary(DailyReportDTO report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"تقرير المخزون اليومي ({report.From:yyyy-MM-dd} - {report.To:yyyy-MM-dd})");
        sb.AppendLine();
        sb.AppendLine($"عدد المنتجات: {report.TotalProducts}");
        sb.AppendLine($"عدد حركات المخزون: {report.MovementsCount}");
        sb.AppendLine();

        if (report.LowStockProducts.Any())
        {
            sb.AppendLine("منتجات وصلت لحد إعادة الطلب:");
            foreach (var p in report.LowStockProducts)
                sb.AppendLine($"- {p.Name}: الكمية {p.Quantity} (حد الطلب {p.ReorderLevel})");
        }
        else
        {
            sb.AppendLine("لا يوجد منتجات وصلت لحد إعادة الطلب.");
        }

        return sb.ToString();
    }
}