namespace InventoryManagementSystem.BLL.DTOs;

public class DashboardStockMovementDTO
{
    public string Month { get; set; } = null!;
    public int Inbound { get; set; }
    public int Outbound { get; set; }
}