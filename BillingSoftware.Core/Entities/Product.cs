using System;

namespace BillingSoftware.Core.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid UnitId { get; set; }
    public string HSNCode { get; set; } = string.Empty;
    public decimal GSTPercentage { get; set; } // e.g. 5, 12, 18, 28
    public decimal PurchasePrice { get; set; }
    public decimal SalesPrice { get; set; }
    public decimal OpeningStock { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual Unit? Unit { get; set; }
}
