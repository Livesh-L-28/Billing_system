using System;

namespace BillingSoftware.Core.Entities;

public class BackupHistory
{
    public Guid Id { get; set; }
    public DateTime BackupTime { get; set; } = DateTime.UtcNow;
    public string BackupFilePath { get; set; } = string.Empty;
    public string BackupType { get; set; } = "Manual"; // Manual, Auto
    public string Status { get; set; } = "Success"; // Success, Failed
    public string ErrorMessage { get; set; } = string.Empty;
}
