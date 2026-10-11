using Microsoft.AspNetCore.Identity;

namespace InventoryManagementSystem.DAL.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}