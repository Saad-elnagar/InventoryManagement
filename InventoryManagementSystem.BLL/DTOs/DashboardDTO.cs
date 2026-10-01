namespace InventoryManagementSystem.BLL.DTOs;

public class DashboardDTO
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalSuppliers { get; set; }
    public int TotalCustomers { get; set; }

    public int TotalStock { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }

    public decimal InventoryValue { get; set; }

    public decimal TotalPurchases { get; set; }
    public decimal TotalSales { get; set; }

    public List<DashboardStockMovementDTO> StockMovements { get; set; } = [];
    public List<DashboardCategoryDTO> CategoryStatistics { get; set; } = [];
    public List<DashboardProductDTO> Products { get; set; } = [];
}