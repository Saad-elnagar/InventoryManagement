using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.DAL.Entities;

[Table("Categories")]
public partial class Category : BaseEntity
{
    [Key]
    [Column("CategoryID")]
    public new int Id { get; set; }

    [Column("CategoryName")]
    public string CategoryName { get; set; } = null!;

    [Column("Description")]
    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}