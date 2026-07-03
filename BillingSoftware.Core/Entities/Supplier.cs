using System;

namespace BillingSoftware.Core.Entities;

public class Supplier
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNo { get; set; } = string.Empty;
    public string BankIFSC { get; set; } = string.Empty;
    public decimal OutstandingBalance { get; set; }
    public bool IsActive { get; set; } = true;
}
