using InventoryManagementSystem.BLL.DTOs;

namespace InventoryManagementSystem.BLL.Interfaces;

public interface IDashboardService
{
    Task<DashboardDTO> GetDashboardAsync();
}