using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.DAL.Entities;

public partial class Sale
{
    public int SaleId { get; set; }

    public DateTime SaleDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? CustomerInfo { get; set; }

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
