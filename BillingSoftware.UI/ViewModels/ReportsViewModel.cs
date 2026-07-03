using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using BillingSoftware.Core.Entities;
using BillingSoftware.Application.Services;
using BillingSoftware.Database;
using BillingSoftware.Reporting;

namespace BillingSoftware.UI.ViewModels;

public class ReportsViewModel : ViewModelBase
{
    private readonly BillingDbContext _dbContext;
    private readonly IAuditLogger _auditLogger;

    // Filter Fields
    private DateTime _startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    private DateTime _endDate = DateTime.Today;
    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    // Report DTOs Lists
    private ObservableCollection<B2BReportRow> _b2bSales = new();
    public ObservableCollection<B2BReportRow> B2BSales
    {
        get => _b2bSales;
        set => SetProperty(ref _b2bSales, value);
    }

    private ObservableCollection<B2CReportRow> _b2cSales = new();
    public ObservableCollection<B2CReportRow> B2CSales
    {
        get => _b2cSales;
        set => SetProperty(ref _b2cSales, value);
    }

    private ObservableCollection<HsnReportRow> _hsnSummary = new();
    public ObservableCollection<HsnReportRow> HsnSummary
    {
        get => _hsnSummary;
        set => SetProperty(ref _hsnSummary, value);
    }

    private ObservableCollection<PurchaseReportRow> _purchases = new();
    public ObservableCollection<PurchaseReportRow> Purchases
    {
        get => _purchases;
        set => SetProperty(ref _purchases, value);
    }

    // Commands
    public ICommand GenerateReportCommand { get; }
    public ICommand ExportExcelCommand { get; }

    public ReportsViewModel(BillingDbContext dbContext, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _auditLogger = auditLogger;

        GenerateReportCommand = new AsyncRelayCommand(GenerateReportsAsync);
        ExportExcelCommand = new RelayCommand(ExportToExcel);

        Title = "GST Reports Console";
        _ = GenerateReportsAsync();
    }

    public async Task GenerateReportsAsync()
    {
        IsBusy = true;
        try
        {
            DateTime start = StartDate.Date;
            DateTime end = EndDate.Date.AddDays(1).AddTicks(-1);

            // 1. GSTR-1 B2B (Sales to Registered Customers)
            var b2b = await _dbContext.Invoices
                .Include(i => i.Customer)
                .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end && !i.IsCancelled &&
                            i.Customer != null && i.Customer.CustomerType == "Registered")
                .OrderBy(i => i.InvoiceDate)
                .Select(i => new B2BReportRow
                {
                    InvoiceDate = i.InvoiceDate,
                    InvoiceNo = i.FormattedInvoiceNumber,
                    CustomerName = i.Customer!.Name,
                    CustomerGSTIN = i.Customer!.GSTIN,
                    PlaceOfSupply = i.Customer!.State,
                    TaxableValue = i.SubTotal,
                    CGST = i.CGSTTotal,
                    SGST = i.SGSTTotal,
                    IGST = i.IGSTTotal,
                    InvoiceValue = i.GrandTotal
                })
                .ToListAsync();

            B2BSales = new ObservableCollection<B2BReportRow>(b2b);

            // 2. GSTR-1 B2C (Sales to Unregistered Customers grouped by Place of Supply)
            var b2cRaw = await _dbContext.Invoices
                .Include(i => i.Customer)
                .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end && !i.IsCancelled &&
                            i.Customer != null && i.Customer.CustomerType != "Registered")
                .ToListAsync();

            var b2c = b2cRaw
                .GroupBy(i => i.Customer?.State ?? "Unknown")
                .Select(g => new B2CReportRow
                {
                    PlaceOfSupply = g.Key,
                    TaxableValue = g.Sum(i => i.SubTotal),
                    CGST = g.Sum(i => i.CGSTTotal),
                    SGST = g.Sum(i => i.SGSTTotal),
                    IGST = g.Sum(i => i.IGSTTotal),
                    InvoiceValue = g.Sum(i => i.GrandTotal)
                })
                .OrderBy(b => b.PlaceOfSupply)
                .ToList();

            B2CSales = new ObservableCollection<B2CReportRow>(b2c);

            // 3. GSTR-1 HSN Summary (Group items by HSN and GST rate)
            var invoiceItems = await _dbContext.InvoiceItems
                .Include(ii => ii.Invoice)
                .Include(ii => ii.Product)
                .ThenInclude(p => p!.Unit)
                .Where(ii => ii.Invoice.InvoiceDate >= start && ii.Invoice.InvoiceDate <= end && !ii.Invoice.IsCancelled)
                .ToListAsync();

            var hsn = invoiceItems
                .GroupBy(ii => new { ii.HSNCode, ii.GSTPercentage })
                .Select(g => new HsnReportRow
                {
                    HSNCode = g.Key.HSNCode ?? "NA",
                    Description = g.First().ProductName,
                    UQC = g.First().Product?.Unit?.Code ?? "PCS",
                    TotalQuantity = g.Sum(ii => ii.Quantity),
                    TotalValue = g.Sum(ii => ii.Rate * ii.Quantity),
                    TaxableValue = g.Sum(ii => ii.TaxableValue),
                    GSTPercentage = g.Key.GSTPercentage,
                    CGST = g.Sum(ii => ii.CGSTAmount),
                    SGST = g.Sum(ii => ii.SGSTAmount),
                    IGST = g.Sum(ii => ii.IGSTAmount),
                    TotalAmount = g.Sum(ii => ii.TotalAmount)
                })
                .OrderBy(h => h.HSNCode)
                .ToList();

            HsnSummary = new ObservableCollection<HsnReportRow>(hsn);

            // 4. GSTR-2 Purchases Report
            var purchasesRaw = await _dbContext.Purchases
                .Include(p => p.Supplier)
                .Where(p => p.PurchaseDate >= start && p.PurchaseDate <= end)
                .OrderBy(p => p.PurchaseDate)
                .Select(p => new PurchaseReportRow
                {
                    PurchaseDate = p.PurchaseDate,
                    InvoiceNo = p.PurchaseNumber,
                    SupplierName = p.Supplier != null ? p.Supplier.Name : "Unknown Supplier",
                    SupplierGSTIN = p.Supplier != null ? p.Supplier.GSTIN : "NA",
                    TaxableValue = p.SubTotal,
                    CGST = p.CGSTTotal,
                    SGST = p.SGSTTotal,
                    IGST = p.IGSTTotal,
                    InvoiceValue = p.GrandTotal
                })
                .ToListAsync();

            Purchases = new ObservableCollection<PurchaseReportRow>(purchasesRaw);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to calculate report data: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExportToExcel()
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"GST_Reports_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                using var package = new OfficeOpenXml.ExcelPackage();
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                // Tab 1: GSTR-1 B2B
                var wsB2B = package.Workbook.Worksheets.Add("GSTR1 B2B");
                WriteSheetHeader(wsB2B, "GSTR-1 B2B (Registered Sales)");
                WriteSheetData(wsB2B, B2BSales);

                // Tab 2: GSTR-1 B2C
                var wsB2C = package.Workbook.Worksheets.Add("GSTR1 B2C");
                WriteSheetHeader(wsB2C, "GSTR-1 B2C (Unregistered POS Sales)");
                WriteSheetData(wsB2C, B2CSales);

                // Tab 3: GSTR-1 HSN Summary
                var wsHSN = package.Workbook.Worksheets.Add("GSTR1 HSN Summary");
                WriteSheetHeader(wsHSN, "GSTR-1 HSN-wise Summary");
                WriteSheetData(wsHSN, HsnSummary);

                // Tab 4: GSTR-2 Purchases
                var wsPurch = package.Workbook.Worksheets.Add("GSTR2 Purchases");
                WriteSheetHeader(wsPurch, "GSTR-2 Purchases Register");
                WriteSheetData(wsPurch, Purchases);

                var fileInfo = new System.IO.FileInfo(saveDialog.FileName);
                package.SaveAs(fileInfo);

                System.Windows.MessageBox.Show("All GST sheets exported successfully to Excel.", "Export Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                _auditLogger.LogActivityAsync("Export GST Reports", "Reports", $"Exported sheets from {StartDate:dd-MM-yyyy} to {EndDate:dd-MM-yyyy}").Wait();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Excel sheet compilation failed: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void WriteSheetHeader(OfficeOpenXml.ExcelWorksheet ws, string title)
    {
        ws.Cells["A1"].Value = title;
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Size = 14;
    }

    private void WriteSheetData<T>(OfficeOpenXml.ExcelWorksheet ws, ObservableCollection<T> data)
    {
        var properties = typeof(T).GetProperties().ToList();
        
        // Headers on row 3
        for (int i = 0; i < properties.Count; i++)
        {
            ws.Cells[3, i + 1].Value = properties[i].Name;
            ws.Cells[3, i + 1].Style.Font.Bold = true;
        }

        // Data from row 4
        var list = data.ToList();
        for (int row = 0; row < list.Count; row++)
        {
            for (int col = 0; col < properties.Count; col++)
            {
                var val = properties[col].GetValue(list[row]);
                ws.Cells[row + 4, col + 1].Value = val;
            }
        }
        
        ws.Cells.AutoFitColumns();
    }
}

// Report Data Transfer Objects
public class B2BReportRow
{
    public DateTime InvoiceDate { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerGSTIN { get; set; } = string.Empty;
    public string PlaceOfSupply { get; set; } = string.Empty;
    public decimal TaxableValue { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal IGST { get; set; }
    public decimal InvoiceValue { get; set; }
}

public class B2CReportRow
{
    public string PlaceOfSupply { get; set; } = string.Empty;
    public decimal TaxableValue { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal IGST { get; set; }
    public decimal InvoiceValue { get; set; }
}

public class HsnReportRow
{
    public string HSNCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UQC { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal GSTPercentage { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal IGST { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PurchaseReportRow
{
    public DateTime PurchaseDate { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierGSTIN { get; set; } = string.Empty;
    public decimal TaxableValue { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal IGST { get; set; }
    public decimal InvoiceValue { get; set; }
}
