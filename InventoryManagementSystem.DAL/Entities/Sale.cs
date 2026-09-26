using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.DAL.Entities;

public partial class Sale : BaseEntity
{
  

    public DateTime SaleDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? CustomerInfo { get; set; }

    public int? CustomerId { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
