using System;

namespace BillingSoftware.Core.Entities;

public class Role
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Permissions { get; set; } = string.Empty; // JSON-serialized permission list
}
