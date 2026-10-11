using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IAiReportSummarizer
{
    Task<string> SummarizeAsync(DailyReportDTO report);
}