namespace InventoryManagementSystem.BLL.DTOs;

public class DashboardProductDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public string Status { get; set; } = null!;
}