using System;

namespace BillingSoftware.Core.Entities;

public class Stock
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string TransactionType { get; set; } = "Adjustment"; // Opening, Sale, Purchase, Adjustment
    public decimal Quantity { get; set; } // Positive for addition, negative for subtraction
    public Guid? ReferenceId { get; set; } // InvoiceId or PurchaseId
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string Remarks { get; set; } = string.Empty;

    // Navigation property
    public virtual Product? Product { get; set; }
}
