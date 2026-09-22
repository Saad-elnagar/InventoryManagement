using System;
using System.Collections.Generic;

namespace WebApplication2.Models;

public partial class Purchase
{
    public int PurchaseId { get; set; }

    public int SupplierId { get; set; }

    public DateTime PurchaseDate { get; set; }

    public decimal TotalAmount { get; set; }

    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();

    public virtual Supplier Supplier { get; set; } = null!;
}
