namespace InventoryManagementSystem.BLL.DTOs;

public class DashboardCategoryDTO
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int ProductCount { get; set; }
}