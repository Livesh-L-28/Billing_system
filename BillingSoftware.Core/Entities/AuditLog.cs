using System;

namespace BillingSoftware.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Login, etc.
    public string ModuleName { get; set; } = string.Empty; // Customer, Invoice, Settings, etc.
    public string Details { get; set; } = string.Empty;
}
