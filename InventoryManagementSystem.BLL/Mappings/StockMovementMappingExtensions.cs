using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.DAL.Entities;

namespace InventoryManagementSystem.BLL.Mappings;

public static class StockMovementMappingExtensions
{
    public static StockMovementDTO ToDto(
        this StockMovement movement,
        string? productName = null)
    {
        return new StockMovementDTO
        {
            Id = movement.Id,
            ProductId = movement.ProductId,
            ProductName = productName,
            Quantity = movement.Quantity,
            MovementType = movement.MovementType,
            MovementDate = movement.MovementDate,
            ReferenceType = movement.ReferenceType,
            ReferenceId = movement.ReferenceId,
            Reason = movement.Reason,
            Notes = movement.Notes
        };
    }
}
