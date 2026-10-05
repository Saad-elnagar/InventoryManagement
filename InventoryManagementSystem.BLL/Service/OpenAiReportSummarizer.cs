using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.BLL.Service;

public class OpenRouterReportSummarizer : IAiReportSummarizer
{
    private readonly HttpClient _httpClient;
    private readonly MlNetReportSummarizer _forecastSummarizer;
    private readonly ILogger<OpenRouterReportSummarizer> _logger;
    private readonly string _model;

    public OpenRouterReportSummarizer(
        HttpClient httpClient,
        MlNetReportSummarizer forecastSummarizer,
        ILogger<OpenRouterReportSummarizer> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _forecastSummarizer = forecastSummarizer;
        _logger = logger;
        _model =
            configuration["OpenRouter:Model"]
            ?? "gpt-5.2";
    }

    public async Task<string> SummarizeAsync(
        DailyReportDTO report)
    {
        var forecastSummary =
            await _forecastSummarizer.SummarizeAsync(report);

        var reportData = new
        {
            period = new
            {
                from = report.From,
                to = report.To
            },
            totalProducts = report.TotalProducts,
            movementsCount = report.MovementsCount,
            movementsByType = report.MovementsByType,
            lowStockProducts = report.LowStockProducts,
            mlNetForecast = forecastSummary
        };

        var prompt =
            $"""
            You are the AI analyst for an Inventory Management System.

            Create a professional daily inventory report for a manager.

            Requirements:
            - Use only the supplied data.
            - Do not invent numbers, products, dates, or business events.
            - Start with a clear title and reporting period.
            - Give a concise executive summary.
            - Highlight important stock movements.
            - List low-stock products with current quantity and reorder level.
            - Include the ML.NET forecast insights when they are present.
            - Mention risks that are directly supported by the data.
            - Give practical recommendations based only on the supplied data.
            - Keep it concise and readable for an email.
            - Use plain text with simple headings and bullet points.
            - Write in clear Egyptian Arabic because this report is for the local management team.

            REPORT DATA:
            {JsonSerializer.Serialize(reportData)}
            """;

        try
        {
            var payload = new
            {
                model = _model,
                instructions =
                    "You are a precise, read-only inventory reporting assistant.",
                input = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                max_output_tokens = 1200
            };

            using var response =
                await _httpClient.PostAsJsonAsync(
                    "responses",
                    payload);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "OpenRouter report request failed. Status: {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    responseBody);

                return BuildFallbackReport(
                    report,
                    forecastSummary);
            }

            using var document =
                JsonDocument.Parse(responseBody);

            var content =
                ExtractText(document.RootElement);

            return string.IsNullOrWhiteSpace(content)
                ? BuildFallbackReport(report, forecastSummary)
                : content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "OpenRouter report summarization failed.");

            return BuildFallbackReport(
                report,
                forecastSummary);
        }
    }

    private static string? ExtractText(
        JsonElement root)
    {
        if (root.TryGetProperty(
                "output_text",
                out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            var text = outputText.GetString();

            if (!string.IsNullOrWhiteSpace(text))
                return text.Trim();
        }

        if (!root.TryGetProperty(
                "output",
                out var output) ||
            output.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty(
                    "content",
                    out var content) ||
                content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var part in content.EnumerateArray())
            {
                if (!part.TryGetProperty(
                        "type",
                        out var type) ||
                    type.GetString() != "output_text")
                    continue;

                if (!part.TryGetProperty(
                        "text",
                        out var textElement))
                    continue;

                var text = textElement.GetString();

                if (!string.IsNullOrWhiteSpace(text))
                    return text.Trim();
            }
        }

        return null;
    }

    private static string BuildFallbackReport(
        DailyReportDTO report,
        string forecastSummary)
    {
        var builder = new StringBuilder();

        builder.AppendLine("تقرير المخزون اليومي");
        builder.AppendLine(
            $"الفترة: {report.From:yyyy-MM-dd HH:mm} إلى {report.To:yyyy-MM-dd HH:mm}");
        builder.AppendLine();
        builder.AppendLine("الملخص:");
        builder.AppendLine($"- إجمالي المنتجات: {report.TotalProducts}");
        builder.AppendLine($"- عدد حركات المخزون: {report.MovementsCount}");
        builder.AppendLine();

        if (report.MovementsByType.Count > 0)
        {
            builder.AppendLine("حركات المخزون:");

            foreach (var movement in report.MovementsByType.Take(5))
            {
                builder.AppendLine(
                    $"- {movement.MovementType}: {movement.Count} حركة، {Math.Abs(movement.TotalQuantity)} وحدة");
            }

            builder.AppendLine();
        }

        if (report.LowStockProducts.Count > 0)
        {
            builder.AppendLine("المنتجات منخفضة المخزون:");

            foreach (var product in report.LowStockProducts.Take(10))
            {
                builder.AppendLine(
                    $"- {product.Name}: الحالي {product.Quantity}، حد إعادة الطلب {product.ReorderLevel}");
            }
        }
        else
        {
            builder.AppendLine(
                "لا توجد منتجات تحت حد إعادة الطلب حاليًا.");
        }

        builder.AppendLine();
        builder.AppendLine("توقعات ML.NET:");
        builder.AppendLine(forecastSummary);

        return builder.ToString().Trim();
    }
}
