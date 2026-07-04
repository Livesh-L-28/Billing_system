using System;

namespace BillingSoftware.Core.Entities;

public class Company
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public string PAN { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankIFSC { get; set; } = string.Empty;
    public string BankBranch { get; set; } = "Main Branch";
    public string TermsAndConditions { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? SignaturePath { get; set; }
    public string State { get; set; } = string.Empty; // Essential to calculate CGST/SGST vs IGST
}
