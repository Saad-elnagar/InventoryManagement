using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.DAL.Entities;

public partial class SupplierProduct : BaseEntity
{
  
    public int SupplierId { get; set; }

    public int ProductId { get; set; }

    public string? SupplierSku { get; set; }

    public decimal ContractPrice { get; set; }

    public int LeadTimeDays { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}
