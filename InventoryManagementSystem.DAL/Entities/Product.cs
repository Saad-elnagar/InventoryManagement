using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.DAL.Entities;

public partial class Product : BaseEntity
{
 

    public string Sku { get; set; } = null!;
    
   
    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public decimal UnitPrice { get; set; }

    public int StockQuantity { get; set; }

    public int LowStockThreshold { get; set; }

    public string? Description { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
}
