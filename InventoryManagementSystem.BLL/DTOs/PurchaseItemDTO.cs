using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.BLL.DTOs;

public class PurchaseItemDTO
{
    public int Id { get; set; }

    [Required]
    public int PurchaseId { get; set; }

    [Required]
    public int ProductId { get; set; }

    public string? ProductName { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public decimal Total =>
        Quantity * UnitPrice;
}