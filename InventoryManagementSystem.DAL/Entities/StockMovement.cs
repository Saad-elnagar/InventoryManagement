using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.DAL.Entities;

public partial class StockMovement : BaseEntity
{
   

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public string MovementType { get; set; } = null!;

    public DateTime MovementDate { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public string? Reason { get; set; }

    public string? Notes { get; set; }

    public virtual Product Product { get; set; } = null!;
}
