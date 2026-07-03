using System;

namespace BillingSoftware.Core.Entities;

public class FinancialYear
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "2026-27"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; } // If locked, transactions are read-only
}
