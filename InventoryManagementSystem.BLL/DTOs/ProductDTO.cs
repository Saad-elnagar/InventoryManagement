using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.BLL.DTOs;

public class ProductDTO
{
    public int Id { get; set; }
    public string SKU { get; set; }  

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }
}