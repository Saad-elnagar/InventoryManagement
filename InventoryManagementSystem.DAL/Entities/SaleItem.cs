using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.DAL.Entities;

public partial class SaleItem : BaseEntity
{
    

    public int SaleId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Sale Sale { get; set; } = null!;
}
