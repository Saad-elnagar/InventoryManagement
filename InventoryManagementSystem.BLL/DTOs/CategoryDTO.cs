using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.BLL.DTOs;

public class CategoryDTO
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }
}