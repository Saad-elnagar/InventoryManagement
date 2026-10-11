using System.ComponentModel.DataAnnotations;
using InventoryManagementSystem.BLL.Enums;

namespace InventoryManagementSystem.BLL.DTOs;

public class CreateStockMovementDTO
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    public StockMovementType? MovementType { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public StockMovementDirection Direction { get; set; }
        = StockMovementDirection.Decrease;

    [Required]
    [MaxLength(250)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
