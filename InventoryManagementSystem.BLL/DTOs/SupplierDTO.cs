using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.BLL.DTOs;

public class SupplierDTO
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string SupplierName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;
}
