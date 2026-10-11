using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.Models.Reports;

public class DailyReportPreviewViewModel
{
    public DailyReportDTO Report { get; set; } = new();

    public string Summary { get; set; } = string.Empty;
}