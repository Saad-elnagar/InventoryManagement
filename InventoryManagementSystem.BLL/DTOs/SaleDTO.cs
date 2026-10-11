using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.BLL.DTOs;

public class SaleDTO
{
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public List<SaleItemDTO> Items { get; set; } = new();
}