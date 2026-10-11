using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.BLL.DTOs;

public class PurchaseDTO
{
    public int Id { get; set; }

    [Required]
    public int SupplierId { get; set; }

    public string? SupplierName { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public List<PurchaseItemDTO> Items { get; set; } = new();
}