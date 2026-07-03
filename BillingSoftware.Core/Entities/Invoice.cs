using System;
using System.Collections.Generic;

namespace BillingSoftware.Core.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public int InvoiceNumber { get; set; } // Sequential number within the financial year
    public string FormattedInvoiceNumber { get; set; } = string.Empty; // e.g. "INV-2026-0001"
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public Guid FinancialYearId { get; set; }
    public Guid CustomerId { get; set; }
    public string BillingAddress { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string POInvoiceNumber { get; set; } = string.Empty;
    public DateTime? PODate { get; set; }
    public DateTime? DueDate { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank, Credit
    public decimal SubTotal { get; set; } // Sum of Taxable values
    public decimal DiscountAmount { get; set; }
    public decimal CGSTTotal { get; set; }
    public decimal SGSTTotal { get; set; }
    public decimal IGSTTotal { get; set; }
    public decimal RoundOff { get; set; }
    public decimal GrandTotal { get; set; }
    public bool IsCancelled { get; set; }
    public string CancelReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public virtual FinancialYear? FinancialYear { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
