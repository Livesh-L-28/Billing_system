using System;

namespace BillingSoftware.Core.Entities;

public class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty; // Snapshot of product name
    public string HSNCode { get; set; } = string.Empty; // Snapshot of HSN
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxableValue { get; set; } // (Quantity * Rate) - DiscountAmount
    public decimal GSTPercentage { get; set; }
    public decimal CGSTAmount { get; set; }
    public decimal SGSTAmount { get; set; }
    public decimal IGSTAmount { get; set; }
    public decimal TotalAmount { get; set; } // TaxableValue + taxes

    // Navigation properties
    public virtual Invoice? Invoice { get; set; }
    public virtual Product? Product { get; set; }
}
