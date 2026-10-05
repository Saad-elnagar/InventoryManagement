using System.Text;
using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace InventoryManagementSystem.BLL.Service;

public class MlNetReportSummarizer : IAiReportSummarizer
{
    private const int HistoryDays = 90;
    private const int ForecastHorizonDays = 7;
    private const int WindowSize = 7;
    private const int SeriesLength = 30;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MlNetReportSummarizer> _logger;
    private readonly MLContext _mlContext = new(seed: 1);

    public MlNetReportSummarizer(IUnitOfWork unitOfWork, ILogger<MlNetReportSummarizer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<string> SummarizeAsync(DailyReportDTO report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("تقرير المخزون الذكي");
        builder.AppendLine($"الفترة: {report.From:yyyy-MM-dd HH:mm} إلى {report.To:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"عدد المنتجات: {report.TotalProducts}");
        builder.AppendLine($"عدد حركات المخزون: {report.MovementsCount}");
        builder.AppendLine();

        if (report.MovementsByType.Any())
        {
            builder.AppendLine("حركات المخزون:");
            foreach (var movement in report.MovementsByType.Take(5))
                builder.AppendLine($"- {movement.MovementType}: {movement.Count} حركة، {Math.Abs(movement.TotalQuantity)} وحدة");
            builder.AppendLine();
        }

        if (!report.LowStockProducts.Any())
        {
            builder.AppendLine("لا توجد منتجات تحت حد إعادة الطلب حاليًا.");
            return builder.ToString().Trim();
        }

        builder.AppendLine("توقع الطلب للأصناف منخفضة المخزون خلال الـ7 أيام القادمة:");

        foreach (var product in report.LowStockProducts)
        {
            try
            {
                var forecast = await ForecastProductAsync(product);

                builder.AppendLine(
                    $"- {product.Name}: الحالي {product.Quantity}، المتوقع {forecast.TotalForecastedDemand} وحدة.");

                if (forecast.WillRunOut)
                    builder.AppendLine(
                        $"  تحذير: قد ينفد المخزون خلال {forecast.DaysUntilStockout} يوم. " +
                        $"الطلب المقترح: {forecast.RecommendedOrderQuantity} وحدة.");
                else if (forecast.ProjectedEndingStock <= product.ReorderLevel)
                    builder.AppendLine(
                        $"  تنبيه: المتوقع بنهاية 7 أيام {forecast.ProjectedEndingStock} وحدة، عند/تحت حد إعادة الطلب.");
                else
                    builder.AppendLine("  الحالة: المخزون المتوقع يكفي لفترة التوقع.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ML.NET forecast failed for product {ProductId}.", product.ProductId);
                builder.AppendLine($"- {product.Name}: تعذر حساب التوقع.");
            }
        }

        builder.AppendLine();
        builder.AppendLine("التوقع مبني على تاريخ المبيعات المسجل في النظام.");
        return builder.ToString().Trim();
    }

    private async Task<ForecastResult> ForecastProductAsync(LowStockItemDTO product)
    {
        var today = DateTime.UtcNow.Date;
        var fromDate = today.AddDays(-(HistoryDays - 1));

        var movements = await _unitOfWork.GenaricRepository<StockMovement>()
            .GetWhereAsync(x =>
                x.ProductId == product.ProductId &&
                x.MovementDate >= fromDate &&
                x.MovementDate < today.AddDays(1));

        var dailyDemand = Enumerable.Range(0, HistoryDays)
            .Select(i =>
            {
                var day = fromDate.AddDays(i);
                var demand = movements
                    .Where(x =>
                        x.MovementDate.Date == day &&
                        string.Equals(x.MovementType, "Sale", StringComparison.OrdinalIgnoreCase) &&
                        x.Quantity < 0)
                    .Sum(x => Math.Abs(x.Quantity));

                return new DemandData { Date = day, Demand = demand };
            })
            .ToList();

        var average = dailyDemand.Average(x => x.Demand);

        float[] predicted;

        if (average <= 0)
        {
            predicted = new float[ForecastHorizonDays];
        }
        else
        {
            predicted = ForecastWithMlNet(dailyDemand);
        }

        var total = predicted.Sum();
        var projectedEndingStock = Math.Max(0, product.Quantity - total);

        int? daysUntilStockout = null;
        var cumulative = 0f;

        for (var i = 0; i < predicted.Length; i++)
        {
            cumulative += predicted[i];
            if (cumulative >= product.Quantity)
            {
                daysUntilStockout = i + 1;
                break;
            }
        }

        var recommendedOrderQuantity = Math.Max(
            0,
            (int)Math.Ceiling(total + product.ReorderLevel - product.Quantity));

        return new ForecastResult(
            (int)Math.Ceiling(total),
            (int)Math.Floor(projectedEndingStock),
            daysUntilStockout,
            daysUntilStockout.HasValue,
            recommendedOrderQuantity);
    }

    private float[] ForecastWithMlNet(List<DemandData> history)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(history);

        var pipeline = _mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(ForecastOutput.ForecastedDemand),
            inputColumnName: nameof(DemandData.Demand),
            windowSize: WindowSize,
            seriesLength: SeriesLength,
            trainSize: history.Count,
            horizon: ForecastHorizonDays,
            confidenceLevel: 0.95f,
            confidenceLowerBoundColumn: nameof(ForecastOutput.LowerBound),
            confidenceUpperBoundColumn: nameof(ForecastOutput.UpperBound));

        var model = pipeline.Fit(dataView);
        var engine = model.CreateTimeSeriesEngine<DemandData, ForecastOutput>(_mlContext);
        var prediction = engine.Predict();

        return prediction.ForecastedDemand.Select(x => Math.Max(0, x)).ToArray();
    }

    private sealed class DemandData
    {
        public DateTime Date { get; set; }
        public float Demand { get; set; }
    }

    private sealed class ForecastOutput
    {
        [ColumnName("ForecastedDemand")]
        public float[] ForecastedDemand { get; set; } = [];
        public float[] LowerBound { get; set; } = [];
        public float[] UpperBound { get; set; } = [];
    }

    private sealed record ForecastResult(
        int TotalForecastedDemand,
        int ProjectedEndingStock,
        int? DaysUntilStockout,
        bool WillRunOut,
        int RecommendedOrderQuantity);
}