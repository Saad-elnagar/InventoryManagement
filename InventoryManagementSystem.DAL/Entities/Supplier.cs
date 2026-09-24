using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.DAL.Entities;

public partial class Supplier :BaseEntitiy
{
   

    public string SupplierName { get; set; } = null!;

    public string ContactName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Address { get; set; } = null!;

    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
}
