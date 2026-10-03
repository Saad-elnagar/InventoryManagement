using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.DAL.Entities;

public partial class Category : BaseEntity
{

    [Column("CategoryName")]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
