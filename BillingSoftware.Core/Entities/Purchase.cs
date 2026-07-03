using System;
using System.Collections.Generic;

namespace BillingSoftware.Core.Entities;

public class Purchase
{
    public Guid Id { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty; // Supplier's bill number
    public DateTime PurchaseDate { get; set; } = DateTime.Today;
    public Guid SupplierId { get; set; }
    public Guid FinancialYearId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal CGSTTotal { get; set; }
    public decimal SGSTTotal { get; set; }
    public decimal IGSTTotal { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Supplier? Supplier { get; set; }
    public virtual FinancialYear? FinancialYear { get; set; }
    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
}
