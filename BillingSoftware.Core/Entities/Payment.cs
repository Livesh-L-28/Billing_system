using System;

namespace BillingSoftware.Core.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal AmountPaid { get; set; }
    public string PaymentMode { get; set; } = "Cash"; // Cash, Bank, Cheque, UPI
    public string TransactionReference { get; set; } = string.Empty;

    // Navigation property
    public virtual Invoice? Invoice { get; set; }
}
