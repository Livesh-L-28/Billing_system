using System;

namespace BillingSoftware.Core.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public string PAN { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int CreditDays { get; set; }
    public string CustomerType { get; set; } = "Unregistered"; // Registered, Unregistered, Composition
    public bool IsActive { get; set; } = true;
}
