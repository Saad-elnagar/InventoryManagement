using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.BLL.DTOs;

public class SupplierDTO 
{
    [Required]
    public  int Id { get; set; }
    [Required]
    public string SupplierName { get; set; } = string.Empty;

    [Required]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;
}