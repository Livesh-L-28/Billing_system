using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BillingSoftware.Core.Entities;
using BillingSoftware.Common;

namespace BillingSoftware.Printing;

public class InvoiceDocument : IDocument
{
    public Invoice Invoice { get; }
    public Company Company { get; }

    public InvoiceDocument(Invoice invoice, Company company)
    {
        Invoice = invoice;
        Company = company;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0.5f, QuestPDF.Infrastructure.Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8));

            page.Content().Border(1).BorderColor(Colors.Black).Column(column =>
            {
                // 1. Header (Company details & Logo)
                column.Item().Element(ComposeHeader);
                column.Item().LineHorizontal(1).LineColor(Colors.Black);

                // 2. Customer billing & shipping info + Invoice Meta
                column.Item().Element(ComposeCustomerAndMetadata);
                column.Item().LineHorizontal(1).LineColor(Colors.Black);

                // 3. Line Items Grid
                column.Item().Element(ComposeTable);
                column.Item().LineHorizontal(1).LineColor(Colors.Black);

                // 4. Bank details & Totals
                column.Item().Element(ComposeBankAndTotals);
                column.Item().LineHorizontal(1).LineColor(Colors.Black);

                // 5. Amount in Words
                column.Item().Element(ComposeAmountInWords);
                column.Item().LineHorizontal(1).LineColor(Colors.Black);

                // 6. HSN Summary table
                column.Item().Element(ComposeHsnSummary);
                column.Item().LineHorizontal(1).LineColor(Colors.Black);

                // 7. Footer text & signature blocks
                column.Item().Element(ComposeFooterSection);
            });

            // Page Numbering Footer outside the main bordered frame
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            // Company Logo box (left)
            row.ConstantItem(85).BorderRight(1).BorderColor(Colors.Black).Padding(5).AlignMiddle().AlignCenter().Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(Company.LogoPath) && File.Exists(Company.LogoPath))
                {
                    try
                    {
                        col.Item().Image(Company.LogoPath);
                    }
                    catch
                    {
                        col.Item().Text("LOGO").FontSize(10).Bold().FontColor(Colors.Grey.Medium);
                    }
                }
                else
                {
                    col.Item().Text("LOGO").FontSize(10).Bold().FontColor(Colors.Grey.Medium);
                }
            });

            // Company Title and details (center)
            row.RelativeItem().Padding(4).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().AlignCenter().Text("TAX INVOICE").Bold().FontSize(11);
                });

                col.Item().AlignCenter().Text(Company.CompanyName).Bold().FontSize(17);
                col.Item().AlignCenter().Text($"Address : {Company.Address}").FontSize(7.5f);
                col.Item().AlignCenter().Text($"Phone No : {Company.Phone} Email : {Company.Email}").FontSize(7.5f);
                col.Item().AlignCenter().Text($"GSTIN : {Company.GSTIN}").Bold().FontSize(8.5f);
            });

            // Right column label
            row.ConstantItem(120).BorderLeft(1).BorderColor(Colors.Black).Padding(4).AlignTop().Column(col =>
            {
                col.Item().Text("ORIGINAL FOR RECIPIENT").Bold().FontSize(7).AlignCenter();
            });
        });
    }

    private void ComposeCustomerAndMetadata(IContainer container)
    {
        container.Row(row =>
        {
            // Customer Info column (left)
            row.RelativeItem().Column(col =>
            {
                // INVOICE TO header
                col.Item().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Black).PaddingHorizontal(5).PaddingVertical(2).Text("INVOICE TO").Bold().FontSize(7.5f);
                
                col.Item().Padding(4).Column(custCol =>
                {
                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(50).Text("Name").FontSize(7.5f);
                        r.RelativeItem().Text($": {Invoice.Customer?.Name}").Bold().FontSize(7.5f);
                    });
                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(50).Text("Address").FontSize(7.5f);
                        r.RelativeItem().Text($": {Invoice.BillingAddress}").FontSize(7.5f);
                    });
                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(50).Text("GST No").FontSize(7.5f);
                        r.RelativeItem().Text($": {Invoice.Customer?.GSTIN}").Bold().FontSize(7.5f);
                    });
                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(50).Text("Tel").FontSize(7.5f);
                        r.RelativeItem().Text($": {Invoice.Customer?.Mobile ?? "-"}").FontSize(7.5f);
                    });
                });

                col.Item().LineHorizontal(1).LineColor(Colors.Black);

                // SHIPPED TO (CONSIGNEE) header
                col.Item().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Black).PaddingHorizontal(5).PaddingVertical(2).Text("SHIPPED TO ( CONSIGNEE )").Bold().FontSize(7.5f);
                
                col.Item().Padding(4).Column(shipCol =>
                {
                    shipCol.Item().Row(r =>
                    {
                        r.ConstantItem(50).Text("Name").FontSize(7.5f);
                        r.RelativeItem().Text($": {Invoice.Customer?.Name}").Bold().FontSize(7.5f);
                    });
                    shipCol.Item().Row(r =>
                    {
                        r.ConstantItem(50).Text("Address").FontSize(7.5f);
                        r.RelativeItem().Text($": {Invoice.ShippingAddress}").FontSize(7.5f);
                    });
                    shipCol.Item().Row(r =>
                    {
                        r.ConstantItem(50).Text("GST No").FontSize(7.5f);
                        r.RelativeItem().Text($": {Invoice.Customer?.GSTIN}").Bold().FontSize(7.5f);
                    });
                    shipCol.Item().Row(r =>
                    {
                        r.ConstantItem(50).Text("Tel").FontSize(7.5f);
                        r.RelativeItem().Text($": {Invoice.Customer?.Mobile ?? "-"}").FontSize(7.5f);
                    });
                });
            });

            // Metadata column (right)
            row.ConstantItem(220).BorderLeft(1).BorderColor(Colors.Black).Column(col =>
            {
                col.Item().Padding(4).Column(metaCol =>
                {
                    AddMetaRow(metaCol, "Invoice Number", Invoice.FormattedInvoiceNumber, true);
                    AddMetaRow(metaCol, "Invoice Date", Invoice.InvoiceDate.ToString("dd/MM/yyyy"));
                    
                    // PO Info
                    AddMetaRow(metaCol, "PO Number", string.IsNullOrWhiteSpace(Invoice.POInvoiceNumber) ? "0" : Invoice.POInvoiceNumber);
                    AddMetaRow(metaCol, "PO Date", Invoice.PODate?.ToString("dd/MM/yyyy") ?? Invoice.InvoiceDate.ToString("dd/MM/yyyy"));
                    
                    // Quotation defaults
                    AddMetaRow(metaCol, "Quotation Number", "0");
                    AddMetaRow(metaCol, "Quotation Date", Invoice.InvoiceDate.ToString("dd/MM/yyyy"));
                    
                    // Place of Origin
                    AddMetaRow(metaCol, "Place of Origin", Invoice.Customer?.State ?? "-");
                    
                    // Bill Type
                    string billType = Invoice.PaymentMethod.Equals("Credit", StringComparison.OrdinalIgnoreCase) ? "CREDIT BILL" : "CASH/BANK BILL";
                    AddMetaRow(metaCol, "Bill Type", billType, true);
                    
                    // Due Days
                    int dueDays = Invoice.Customer?.CreditDays ?? 0;
                    AddMetaRow(metaCol, "Due Days", dueDays > 0 ? dueDays.ToString() : "-");
                });
            });
        });
    }

    private void AddMetaRow(ColumnDescriptor col, string label, string value, bool isBoldValue = false)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(90).Text(label).FontSize(7.5f);
            var valText = r.RelativeItem().Text($": {value}").FontSize(7.5f);
            if (isBoldValue)
            {
                valText.Bold();
            }
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);  // S.NO
                columns.RelativeColumn();    // DESCRIPTION
                columns.ConstantColumn(50);  // HSNC
                columns.ConstantColumn(65);  // QTY
                columns.ConstantColumn(40);  // UOM
                columns.ConstantColumn(70);  // RATE/UNIT
                columns.ConstantColumn(80);  // AMOUNT
            });

            // Table Headers
            table.Header(header =>
            {
                AddHeaderCell(header.Cell(), "S.NO");
                AddHeaderCell(header.Cell(), "DESCRIPTION & SPECIFICATION OF PRODUCT");
                AddHeaderCell(header.Cell(), "HSNC");
                AddHeaderCell(header.Cell(), "QTY");
                AddHeaderCell(header.Cell(), "UOM");
                AddHeaderCell(header.Cell(), "RATE / UNIT");
                AddHeaderCell(header.Cell(), "AMOUNT");
            });

            // Table Rows
            int index = 1;
            foreach (var item in Invoice.InvoiceItems)
            {
                AddTableCell(table.Cell(), index.ToString(), false, false);
                AddTableCell(table.Cell(), item.ProductName, false, true);
                AddTableCell(table.Cell(), item.HSNCode ?? "", false, false);
                AddTableCell(table.Cell(), item.Quantity.ToString("N2"), true, false);
                AddTableCell(table.Cell(), item.Product?.Unit?.Code ?? "PCS", false, false);
                AddTableCell(table.Cell(), item.Rate.ToString("N2"), true, false);
                AddTableCell(table.Cell(), item.TaxableValue.ToString("N2"), true, false);

                index++;
            }

            // Pad with empty rows to sustain vertical borders
            int padRows = Math.Max(0, 27 - Invoice.InvoiceItems.Count);
            for (int i = 0; i < padRows; i++)
            {
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, true);
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, false);
            }
        });
    }

    private void AddHeaderCell(IContainer cell, string text)
    {
        cell.Background(Colors.Grey.Lighten3)
            .BorderVertical(0.5f)
            .BorderBottom(1)
            .BorderColor(Colors.Black)
            .Padding(3)
            .AlignCenter()
            .Text(text)
            .Bold()
            .FontSize(7.5f);
    }

    private void AddTableCell(IContainer cell, string text, bool isRightAlign, bool isLeftAlign)
    {
        var current = cell.BorderVertical(0.5f)
            .BorderColor(Colors.Black)
            .PaddingHorizontal(4)
            .PaddingVertical(3);

        if (isRightAlign)
        {
            current = current.AlignRight();
        }
        else if (isLeftAlign)
        {
            current = current.AlignLeft();
        }
        else
        {
            current = current.AlignCenter();
        }

        current.Text(text).FontSize(7.5f);
    }

    private void ComposeBankAndTotals(IContainer container)
    {
        container.Row(row =>
        {
            // Bank details (left)
            row.RelativeItem().Column(col =>
            {
                col.Item().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Black).PaddingHorizontal(5).PaddingVertical(2).AlignCenter().Text("Company's Bank Details").Bold().FontSize(7.5f);
                
                col.Item().Padding(4).Column(bankCol =>
                {
                    AddBankRow(bankCol, "Name", Company.CompanyName);
                    AddBankRow(bankCol, "Bank Name", Company.BankName);
                    AddBankRow(bankCol, "Branch", "Main Branch");
                    AddBankRow(bankCol, "CA A/c. No.", Company.BankAccountNumber);
                    AddBankRow(bankCol, "IFSC Code", Company.BankIFSC);
                });
            });

            // Invoice Totals (right)
            row.ConstantItem(220).BorderLeft(1).BorderColor(Colors.Black).Column(col =>
            {
                col.Item().Padding(4).Column(totalsCol =>
                {
                    AddTotalRow(totalsCol, "Total Amount", Invoice.SubTotal.ToString("N2"));
                    
                    if (Invoice.CGSTTotal > 0)
                    {
                        AddTotalRow(totalsCol, "Central GST", Invoice.CGSTTotal.ToString("N2"));
                    }
                    if (Invoice.SGSTTotal > 0)
                    {
                        AddTotalRow(totalsCol, "State GST", Invoice.SGSTTotal.ToString("N2"));
                    }
                    if (Invoice.IGSTTotal > 0)
                    {
                        AddTotalRow(totalsCol, "Integrated GST", Invoice.IGSTTotal.ToString("N2"));
                    }
                    
                    AddTotalRow(totalsCol, "Round Off", Invoice.RoundOff.ToString("N2"));
                    
                    totalsCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Grand Invoice Amt :").Bold().FontSize(8.5f);
                        r.ConstantItem(90).Text($"₹ {Invoice.GrandTotal:N2}").Bold().FontSize(8.5f).AlignRight();
                    });
                });
            });
        });
    }

    private void AddBankRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(60).Text(label).FontSize(7.5f);
            r.RelativeItem().Text($": {value}").FontSize(7.5f);
        });
    }

    private void AddTotalRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(r =>
        {
            r.RelativeItem().Text($"{label} :").FontSize(7.5f);
            r.ConstantItem(90).Text(value).FontSize(7.5f).AlignRight();
        });
    }

    private void ComposeAmountInWords(IContainer container)
    {
        container.Background(Colors.Grey.Lighten3).Padding(4).Row(row =>
        {
            string words = NumberToWords.ConvertAmount(Invoice.GrandTotal);
            row.RelativeItem().Text(t =>
            {
                t.Span("Rupees in Words : ").Bold().FontSize(7.5f);
                t.Span(words).Bold().FontSize(7.5f);
            });
        });
    }

    private void ComposeHsnSummary(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.2f); // HSN Code
                columns.RelativeColumn(1.5f); // Taxable Value
                columns.RelativeColumn(0.8f); // CGST %
                columns.RelativeColumn(1.2f); // CGST Amount
                columns.RelativeColumn(0.8f); // SGST %
                columns.RelativeColumn(1.2f); // SGST Amount
                columns.RelativeColumn(0.8f); // IGST %
                columns.RelativeColumn(1.2f); // IGST Amount
            });

            // Table Headers (Multi-row span)
            table.Header(header =>
            {
                header.Cell().RowSpan(2).BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().AlignMiddle().Text("HSN Code").Bold().FontSize(7);
                header.Cell().RowSpan(2).BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().AlignMiddle().Text("Taxable").Bold().FontSize(7);
                
                header.Cell().ColumnSpan(2).BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("Central GST").Bold().FontSize(7);
                header.Cell().ColumnSpan(2).BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("State GST").Bold().FontSize(7);
                header.Cell().ColumnSpan(2).BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("Integrated GST").Bold().FontSize(7);

                header.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("%").Bold().FontSize(7);
                header.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("Amount").Bold().FontSize(7);
                
                header.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("%").Bold().FontSize(7);
                header.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("Amount").Bold().FontSize(7);
                
                header.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("%").Bold().FontSize(7);
                header.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(2).AlignCenter().Text("Amount").Bold().FontSize(7);
            });

            // HSN aggregates
            var groups = Invoice.InvoiceItems
                .GroupBy(i => new { HSN = i.HSNCode ?? "NA", i.GSTPercentage })
                .Select(g => new
                {
                    HSN = g.Key.HSN,
                    GST = g.Key.GSTPercentage,
                    Taxable = g.Sum(i => i.TaxableValue),
                    CGST = g.Sum(i => i.CGSTAmount),
                    SGST = g.Sum(i => i.SGSTAmount),
                    IGST = g.Sum(i => i.IGSTAmount)
                })
                .ToList();

            foreach (var g in groups)
            {
                table.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Padding(2).AlignCenter().Text(g.HSN).FontSize(7);
                table.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Padding(2).AlignRight().Text(g.Taxable.ToString("N2")).FontSize(7);

                // CGST
                decimal cgstRate = g.IGST == 0 ? g.GST / 2 : 0;
                table.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Padding(2).AlignCenter().Text(cgstRate > 0 ? cgstRate.ToString("G29") : "0.00").FontSize(7);
                table.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Padding(2).AlignRight().Text(g.CGST.ToString("N2")).FontSize(7);

                // SGST
                decimal sgstRate = g.IGST == 0 ? g.GST / 2 : 0;
                table.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Padding(2).AlignCenter().Text(sgstRate > 0 ? sgstRate.ToString("G29") : "0.00").FontSize(7);
                table.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Padding(2).AlignRight().Text(g.SGST.ToString("N2")).FontSize(7);

                // IGST
                decimal igstRate = g.IGST > 0 ? g.GST : 0;
                table.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Padding(2).AlignCenter().Text(igstRate > 0 ? igstRate.ToString("G29") : "0.00").FontSize(7);
                table.Cell().BorderRight(0.5f).BorderBottom(1).BorderColor(Colors.Black).Padding(2).AlignRight().Text(g.IGST.ToString("N2")).FontSize(7);
            }
        });
    }

    private void ComposeFooterSection(IContainer container)
    {
        container.Padding(4).Column(col =>
        {
            col.Item().Row(row =>
            {
                // Jurisdiction details (left)
                row.RelativeItem().Column(leftCol =>
                {
                    leftCol.Item().Text("All disputes are subject to local jurisdiction").FontSize(7);
                    leftCol.Item().Height(8);
                    leftCol.Item().Text("E.&.O.E").FontSize(7);
                });

                // Signatory details (right)
                row.RelativeItem().Column(rightCol =>
                {
                    rightCol.Item().Text($"For {Company.CompanyName}").Bold().FontSize(7.5f).AlignRight();
                    rightCol.Item().Height(40); // signature space
                    rightCol.Item().Text("Authorised Signatory").Bold().FontSize(7.5f).AlignRight();
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignRight().Text(x =>
        {
            x.Span("Page ").FontSize(7);
            x.CurrentPageNumber().FontSize(7);
            x.Span(" of ").FontSize(7);
            x.TotalPages().FontSize(7);
        });
    }
}

public static class InvoicePdfGenerator
{
    static InvoicePdfGenerator()
    {
        // Set QuestPDF license for community use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void GeneratePdf(Invoice invoice, Company company, string filePath)
    {
        var document = new InvoiceDocument(invoice, company);
        document.GeneratePdf(filePath);
    }
}
