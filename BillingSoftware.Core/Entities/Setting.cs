using System;

namespace BillingSoftware.Core.Entities;

public class Setting
{
    public int Id { get; set; } // Static ID = 1 for single row settings
    public string InvoicePrefix { get; set; } = "INV";
    public string InvoiceFormat { get; set; } = "{Prefix}-{Year}-{Number:D4}"; // e.g. INV-2026-0001
    public bool EnableRoundOff { get; set; } = true;
    public string Theme { get; set; } = "Light"; // Light or Dark
    public string BackupFolderPath { get; set; } = string.Empty;
    public string DefaultPrinterName { get; set; } = string.Empty;
}
