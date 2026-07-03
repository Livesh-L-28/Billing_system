using System;

namespace BillingSoftware.Core.Entities;

public class Unit
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // e.g. MTR, PCS, KGS
    public string Description { get; set; } = string.Empty;
}
