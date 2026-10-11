using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IReportService
{
    Task<DailyReportDTO> BuildDailyReportAsync();
}