using InventoryManagementSystem.BLL.Enums;

namespace InventoryManagementSystem.BLL.DTOs;

public class StockMovementDTO
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int Quantity { get; set; }

    public StockMovementType MovementType { get; set; }

    public DateTime MovementDate { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }
}