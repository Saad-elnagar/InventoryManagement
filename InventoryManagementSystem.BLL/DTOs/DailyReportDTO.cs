namespace InventoryManagementSystem.BLL.DTOs;

public class DailyReportDTO
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int TotalProducts { get; set; }

    public int MovementsCount { get; set; }

    public List<MovementTypeSummaryDTO> MovementsByType { get; set; } = new();

    public List<LowStockItemDTO> LowStockProducts { get; set; } = new();
}

public class MovementTypeSummaryDTO
{
    public string MovementType { get; set; } = string.Empty;

    public int Count { get; set; }

    public int TotalQuantity { get; set; }
}

public class LowStockItemDTO
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int ReorderLevel { get; set; }
}