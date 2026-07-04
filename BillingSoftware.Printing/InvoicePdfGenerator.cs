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
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9.5f));

            page.Header().PaddingBottom(2).Row(row =>
            {
                row.RelativeItem().PaddingLeft(120).AlignCenter().Text("SRI ANGALA PARAMESHWARI THUNAI").FontSize(8f);
                row.ConstantItem(120).AlignRight().Text("TRIPLICATE FOR SUPPLIER").Bold().FontSize(8f);
            });

            page.Content().Border(1).BorderColor(Colors.Black).Column(column =>
            {
                // 1. Header (Company details & Logo)
                column.Item().Element(ComposeHeader);
                column.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                column.Item().Element(ComposeCompanyTaxContactRow);
                column.Item().LineHorizontal(0.5f).LineColor(Colors.Black);

                // 2. Customer billing info + Invoice Meta
                column.Item().Element(ComposeCustomerAndMetadata);
                column.Item().LineHorizontal(0.5f).LineColor(Colors.Black);

                // 3. Line Items Grid
                column.Item().Element(ComposeTable);
                column.Item().LineHorizontal(0.5f).LineColor(Colors.Black);

                // 4. Bank details & Totals (excluding Net Invoice Value)
                column.Item().Element(ComposeBankAndTotals);
                column.Item().LineHorizontal(0.5f).LineColor(Colors.Black);

                // 5. Rupees in Words & Net Invoice Value
                column.Item().Element(ComposeAmountAndNetValue);
                column.Item().LineHorizontal(0.5f).LineColor(Colors.Black);

                // 6. HSN Summary table
                column.Item().Element(ComposeHsnSummary);
                column.Item().LineHorizontal(0.5f).LineColor(Colors.Black);

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
            row.ConstantItem(85).BorderRight(0.5f).BorderColor(Colors.Black).Padding(5).AlignMiddle().AlignCenter().Column(col =>
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
                // Centered TAX INVOICE box
                col.Item().AlignCenter().Border(0.5f).BorderColor(Colors.Black).PaddingHorizontal(10).PaddingVertical(2).Text("TAX INVOICE").Bold().FontSize(10);
                
                string mainName = Company.CompanyName;
                string subtitle = "";
                if (mainName.StartsWith("S.P.ATHIAPPAN", StringComparison.OrdinalIgnoreCase))
                {
                    mainName = "S.P.ATHIAPPAN";
                    subtitle = "CLOTH MANUFACTURERS";
                }

                col.Item().AlignCenter().Text(mainName).Bold().FontSize(22);
                if (!string.IsNullOrEmpty(subtitle))
                {
                    col.Item().AlignCenter().Text(subtitle).Bold().FontSize(11);
                }

                // Format address parts
                var lines = new List<string>();
                if (Company.Address.Contains("\n"))
                {
                    lines = Company.Address.Split('\n').Select(l => l.Trim()).ToList();
                }
                else
                {
                    var parts = Company.Address.Split(',').Select(p => p.Trim()).ToList();
                    if (parts.Count > 1 && parts[0].Length < 10)
                    {
                        lines.Add(parts[0] + ", " + parts[1] + ",");
                        for (int i = 2; i < parts.Count; i++)
                        {
                            lines.Add(parts[i]);
                        }
                    }
                    else
                    {
                        lines = parts;
                    }
                }

                foreach (var line in lines)
                {
                    col.Item().AlignCenter().Text(line).FontSize(9f);
                }
            });
        });
    }

    private void ComposeCompanyTaxContactRow(IContainer container)
    {
        container.PaddingVertical(3).PaddingHorizontal(8).Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span("GSTIN : ").Bold().FontSize(8.5f);
                t.Span(Company.GSTIN).Bold().FontSize(8.5f);
            });
            row.RelativeItem().AlignCenter().Text(t =>
            {
                t.Span("State  : ").Bold().FontSize(8.5f);
                t.Span(Company.State.ToUpper()).Bold().FontSize(8.5f);
            });
            row.RelativeItem().AlignCenter().Text(t =>
            {
                t.Span("State Code  : ").Bold().FontSize(8.5f);
                string stateCode = Company.GSTIN.Length >= 2 && char.IsDigit(Company.GSTIN[0]) && char.IsDigit(Company.GSTIN[1]) 
                    ? Company.GSTIN.Substring(0, 2) 
                    : "33";
                t.Span(stateCode).Bold().FontSize(8.5f);
            });
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Ph : ").Bold().FontSize(8.5f);
                t.Span(Company.Phone).Bold().FontSize(8.5f);
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
                // Details of Goods Receiver header
                col.Item().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingHorizontal(5).PaddingVertical(2).AlignCenter().Text("Details of Goods Receiver").Bold().FontSize(9f);
                
                col.Item().PaddingVertical(5).PaddingHorizontal(6).Column(custCol =>
                {
                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(60).Text("Name").FontSize(8.5f);
                        r.RelativeItem().Text($": {Invoice.Customer?.Name}").Bold().FontSize(8.5f);
                    });

                    var addrLines = (Invoice.BillingAddress ?? "")
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .ToList();

                    while (addrLines.Count < 3)
                    {
                        addrLines.Add("");
                    }

                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(60).Text("Address").FontSize(8.5f);
                        r.RelativeItem().Text($": {addrLines[0]}").FontSize(8.5f);
                    });
                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(60).Text("").FontSize(8.5f);
                        r.RelativeItem().Text($"  {addrLines[1]}").FontSize(8.5f);
                    });
                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(60).Text("").FontSize(8.5f);
                        r.RelativeItem().Row(addrRow =>
                        {
                            addrRow.RelativeItem().Text($"  {addrLines[2]}").FontSize(8.5f);
                            addrRow.ConstantItem(100).Text($"PIN : {Invoice.Customer?.Pincode ?? "0"}").FontSize(8.5f);
                        });
                    });

                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(60).Text("GSTIN").FontSize(8.5f);
                        r.RelativeItem().Row(gstRow =>
                        {
                            gstRow.RelativeItem().Text($": {Invoice.Customer?.GSTIN}").Bold().FontSize(8.5f);
                            gstRow.ConstantItem(100).Text($"Ph : {Invoice.Customer?.Mobile ?? "0"}").FontSize(8.5f);
                        });
                    });

                    custCol.Item().Row(r =>
                    {
                        r.ConstantItem(60).Text("State").FontSize(8.5f);
                        r.RelativeItem().Row(stateRow =>
                        {
                            stateRow.RelativeItem().Text($": {Invoice.Customer?.State?.ToUpper()}").FontSize(8.5f);
                            
                            string custStateCode = (Invoice.Customer?.GSTIN ?? "").Length >= 2 && char.IsDigit(Invoice.Customer!.GSTIN[0]) && char.IsDigit(Invoice.Customer!.GSTIN[1])
                                ? Invoice.Customer.GSTIN.Substring(0, 2)
                                : "33";
                            stateRow.ConstantItem(100).Text($"State Code : {custStateCode}").FontSize(8.5f);
                        });
                    });
                });
            });

            // Metadata column (right)
            row.ConstantItem(220).BorderLeft(0.5f).BorderColor(Colors.Black).Column(col =>
            {
                col.Item().PaddingVertical(5).PaddingHorizontal(8).Column(metaCol =>
                {
                    AddMetaRow(metaCol, "Invoice No", Invoice.FormattedInvoiceNumber, true);
                    AddMetaRow(metaCol, "Invoice Date", Invoice.InvoiceDate.ToString("dd/MM/yyyy"));
                    
                    string billType = Invoice.PaymentMethod.Equals("Credit", StringComparison.OrdinalIgnoreCase) ? "CREDIT BILL" : "CASH/BANK BILL";
                    AddMetaRow(metaCol, "Bill Type", billType, true);
                    
                    int dueDays = Invoice.Customer?.CreditDays ?? 0;
                    AddMetaRow(metaCol, "Due Days", dueDays > 0 ? dueDays.ToString() : "0");
                    
                    AddMetaRow(metaCol, "D.C. Number", "-");
                    AddMetaRow(metaCol, "Eway Bill No", "");
                });
            });
        });
    }

    private void AddMetaRow(ColumnDescriptor col, string label, string value, bool isBoldValue = true)
    {
        col.Item().PaddingVertical(2.5f).Row(r =>
        {
            r.ConstantItem(105).Text(label).FontSize(8.5f);
            var valText = r.RelativeItem().Text($": {value}").FontSize(8.5f);
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
                columns.ConstantColumn(30);  // S.No.
                columns.ConstantColumn(50);  // Colour
                columns.RelativeColumn();    // Item Description
                columns.ConstantColumn(50);  // HSN
                columns.ConstantColumn(75);  // Qty / Unit
                columns.ConstantColumn(70);  // Rate
                columns.ConstantColumn(80);  // Amount
            });

            // Table Headers
            table.Header(header =>
            {
                AddHeaderCell(header.Cell(), "S.No.");
                AddHeaderCell(header.Cell(), "Colour");
                AddHeaderCell(header.Cell(), "Item Description");
                AddHeaderCell(header.Cell(), "HSN");
                AddHeaderCell(header.Cell(), "Qty / Unit");
                AddHeaderCell(header.Cell(), "Rate");
                AddHeaderCell(header.Cell(), "Amount");
            });

            // Table Rows
            int index = 1;
            foreach (var item in Invoice.InvoiceItems)
            {
                AddTableCell(table.Cell(), index.ToString(), false, false);
                AddTableCell(table.Cell(), "-", false, false);
                AddTableCell(table.Cell(), item.ProductName, false, true);
                AddTableCell(table.Cell(), item.HSNCode ?? "", false, false);
                
                string qtyUnit = $"{item.Quantity:N0} {item.Product?.Unit?.Code ?? "PICES"}";
                AddTableCell(table.Cell(), qtyUnit, true, false);
                
                AddTableCell(table.Cell(), item.Rate.ToString("N2"), true, false);
                AddTableCell(table.Cell(), item.TaxableValue.ToString("N2"), true, false);

                index++;
            }

            // Pad with empty rows to sustain vertical borders
            int padRows = Math.Max(0, 20 - Invoice.InvoiceItems.Count);
            for (int i = 0; i < padRows; i++)
            {
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, true);
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, false);
                AddTableCell(table.Cell(), "", false, false);
            }

            // Totals row at the bottom of the table
            AddTableCell(table.Cell(), "", false, false); // S.No.
            AddTableCell(table.Cell(), "", false, false); // Colour
            AddTableCell(table.Cell(), "", false, true);  // Item Description
            AddTableCell(table.Cell(), "", false, false); // HSN
            
            decimal totalQty = Invoice.InvoiceItems.Sum(item => item.Quantity);
            var totalQtyCell = table.Cell().BorderVertical(0.5f).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingVertical(5.5f).AlignRight();
            totalQtyCell.Text(totalQty.ToString("N0")).Bold().FontSize(9f);
            
            AddTableCell(table.Cell(), "", false, false); // Rate
            
            var totalAmtCell = table.Cell().BorderVertical(0.5f).BorderColor(Colors.Black).PaddingHorizontal(4).PaddingVertical(5.5f).AlignRight();
            totalAmtCell.Text(Invoice.SubTotal.ToString("N2")).Bold().FontSize(9f);
        });
    }

    private void AddHeaderCell(IContainer cell, string text)
    {
        cell.BorderVertical(0.5f)
            .BorderBottom(0.5f)
            .BorderColor(Colors.Black)
            .PaddingVertical(6.5f)
            .PaddingHorizontal(3)
            .AlignCenter()
            .Text(text)
            .Bold()
            .FontSize(8.5f);
    }

    private void AddTableCell(IContainer cell, string text, bool isRightAlign, bool isLeftAlign)
    {
        var current = cell.BorderVertical(0.5f)
            .BorderColor(Colors.Black)
            .PaddingHorizontal(4)
            .PaddingVertical(5.5f);

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

        current.Text(text).FontSize(8.5f);
    }

    private void ComposeBankAndTotals(IContainer container)
    {
        container.Row(row =>
        {
            // Bank details (left)
            row.RelativeItem().Column(col =>
            {
                col.Item().BorderBottom(0.5f).BorderColor(Colors.Black).PaddingHorizontal(5).PaddingVertical(2).AlignCenter().Text("Company's Bank Details").Bold().FontSize(9f);
                
                col.Item().PaddingVertical(5).PaddingHorizontal(6).Column(bankCol =>
                {
                    AddBankRow(bankCol, "Bank Name", Company.BankName);
                    AddBankRow(bankCol, "Branch", string.IsNullOrWhiteSpace(Company.BankBranch) ? "Main Branch" : Company.BankBranch);
                    AddBankRow(bankCol, "CA A/c. No.", Company.BankAccountNumber);
                    AddBankRow(bankCol, "IFSC Code", Company.BankIFSC);
                });
            });

            // Invoice Totals (right)
            row.ConstantItem(220).BorderLeft(0.5f).BorderColor(Colors.Black).Column(col =>
            {
                col.Item().PaddingVertical(5).PaddingHorizontal(8).Column(totalsCol =>
                {
                    decimal totalAmountBeforeDiscount = Invoice.SubTotal + Invoice.DiscountAmount;
                    AddTotalRow(totalsCol, "Total Amount", totalAmountBeforeDiscount.ToString("N2"));
                    AddTotalRow(totalsCol, "Total Discount", Invoice.DiscountAmount.ToString("N2"));
                    AddTotalRow(totalsCol, "Total Taxable", Invoice.SubTotal.ToString("N2"));
                    
                    if (Invoice.IGSTTotal > 0)
                    {
                        AddTotalRow(totalsCol, "IGST Value", Invoice.IGSTTotal.ToString("N2"));
                    }
                    else
                    {
                        AddTotalRow(totalsCol, "CGST Value", Invoice.CGSTTotal.ToString("N2"));
                        AddTotalRow(totalsCol, "SGST Value", Invoice.SGSTTotal.ToString("N2"));
                    }
                    
                    AddTotalRow(totalsCol, "Round Off", Invoice.RoundOff.ToString("N2"));
                });
            });
        });
    }

    private void AddBankRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingVertical(2.5f).Row(r =>
        {
            r.ConstantItem(75).Text(label).FontSize(8.5f);
            r.RelativeItem().Text($": {value}").Bold().FontSize(8.5f);
        });
    }

    private void AddTotalRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingVertical(2.5f).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(8.5f);
            r.ConstantItem(15).Text(":").FontSize(8.5f).AlignCenter();
            r.ConstantItem(80).Text(value).FontSize(8.5f).AlignRight();
        });
    }

    private void ComposeAmountAndNetValue(IContainer container)
    {
        container.Row(row =>
        {
            // Rupees in Words (left)
            row.RelativeItem().PaddingVertical(4).PaddingHorizontal(6).Row(r =>
            {
                string words = NumberToWords.ConvertAmount(Invoice.GrandTotal);
                r.RelativeItem().Text(t =>
                {
                    t.Span("Rupees in Words : ").Bold().FontSize(8.5f);
                    t.Span(words).Bold().FontSize(8.5f);
                });
            });

            // Net Invoice Value (right)
            row.ConstantItem(220).BorderLeft(0.5f).BorderColor(Colors.Black).PaddingVertical(4).PaddingHorizontal(8).Row(r =>
            {
                r.RelativeItem().Text("Net Invoice Value").Bold().FontSize(10.5f);
                r.ConstantItem(15).Text(":").Bold().FontSize(10.5f).AlignCenter();
                r.ConstantItem(80).Text(Invoice.GrandTotal.ToString("N2")).Bold().FontSize(10.5f).AlignRight();
            });
        });
    }

    private void ComposeHsnSummary(IContainer container)
    {
        bool hasIgst = Invoice.IGSTTotal > 0;

        container.Table(table =>
        {
            if (hasIgst)
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f); // HSN Code
                    columns.RelativeColumn(2.0f); // Taxable Value
                    columns.RelativeColumn(1.0f); // IGST %
                    columns.RelativeColumn(1.5f); // IGST Amount
                });

                table.Header(header =>
                {
                    header.Cell().RowSpan(2).BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().AlignMiddle().Text("HSN Code").Bold().FontSize(8.5f);
                    header.Cell().RowSpan(2).BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().AlignMiddle().Text("Taxable").Bold().FontSize(8.5f);
                    
                    header.Cell().ColumnSpan(2).BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().Text("Integrated GST").Bold().FontSize(8.5f);

                    header.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().Text("%").Bold().FontSize(8.5f);
                    header.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().Text("Amount").Bold().FontSize(8.5f);
                });
            }
            else
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f); // HSN Code
                    columns.RelativeColumn(2.0f); // Taxable Value
                    columns.RelativeColumn(1.0f); // CGST %
                    columns.RelativeColumn(1.5f); // CGST Amount
                    columns.RelativeColumn(1.0f); // SGST %
                    columns.RelativeColumn(1.5f); // SGST Amount
                });

                table.Header(header =>
                {
                    header.Cell().RowSpan(2).BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().AlignMiddle().Text("HSN Code").Bold().FontSize(8.5f);
                    header.Cell().RowSpan(2).BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().AlignMiddle().Text("Taxable").Bold().FontSize(8.5f);
                    
                    header.Cell().ColumnSpan(2).BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().Text("Central GST").Bold().FontSize(8.5f);
                    header.Cell().ColumnSpan(2).BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().Text("State GST").Bold().FontSize(8.5f);

                    header.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().Text("%").Bold().FontSize(8.5f);
                    header.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().Text("Amount").Bold().FontSize(8.5f);
                    
                    header.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().Text("%").Bold().FontSize(8.5f);
                    header.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).Padding(2).AlignCenter().Text("Amount").Bold().FontSize(8.5f);
                });
            }

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
                table.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingVertical(3.5f).PaddingHorizontal(2).AlignCenter().Text(g.HSN).FontSize(8.5f);
                table.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingVertical(3.5f).PaddingHorizontal(2).AlignRight().Text(g.Taxable.ToString("N2")).FontSize(8.5f);

                if (hasIgst)
                {
                    // IGST
                    decimal igstRate = g.GST;
                    table.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingVertical(3.5f).PaddingHorizontal(2).AlignCenter().Text(igstRate > 0 ? igstRate.ToString("G29") : "0.00").FontSize(8.5f);
                    table.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingVertical(3.5f).PaddingHorizontal(2).AlignRight().Text(g.IGST.ToString("N2")).FontSize(8.5f);
                }
                else
                {
                    // CGST
                    decimal cgstRate = g.GST / 2;
                    table.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingVertical(3.5f).PaddingHorizontal(2).AlignCenter().Text(cgstRate > 0 ? cgstRate.ToString("G29") : "0.00").FontSize(8.5f);
                    table.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingVertical(3.5f).PaddingHorizontal(2).AlignRight().Text(g.CGST.ToString("N2")).FontSize(8.5f);

                    // SGST
                    decimal sgstRate = g.GST / 2;
                    table.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingVertical(3.5f).PaddingHorizontal(2).AlignCenter().Text(sgstRate > 0 ? sgstRate.ToString("G29") : "0.00").FontSize(8.5f);
                    table.Cell().BorderRight(0.5f).BorderBottom(0.5f).BorderColor(Colors.Black).PaddingVertical(3.5f).PaddingHorizontal(2).AlignRight().Text(g.SGST.ToString("N2")).FontSize(8.5f);
                }
            }
        });
    }

    private void ComposeFooterSection(IContainer container)
    {
        container.PaddingTop(5).PaddingBottom(2).PaddingHorizontal(6).Column(col =>
        {
            col.Item().Row(row =>
            {
                // Left Column: Declaration
                row.RelativeItem(1.4f).Column(leftCol =>
                {
                    leftCol.Item().Text("Declaration").Bold().FontSize(8.5f);
                    leftCol.Item().Text("1. Payment Terms - Immediate").FontSize(8f);
                    leftCol.Item().Text("2. Subject to Rasipuram Jurisdiction only.").FontSize(8f);
                    leftCol.Item().Text("3. Goods once sold cannot be taken back.").FontSize(8f);
                    leftCol.Item().Text("4. Please do not mix 2 different Lots.").FontSize(8f);
                    leftCol.Item().Text("5. No claim will be entertaine.").FontSize(8f);
                });

                // Middle Column: Received By
                row.RelativeItem(0.8f).Column(midCol =>
                {
                    midCol.Item().Height(45); // Space above
                    midCol.Item().AlignCenter().AlignBottom().Text("Received By").FontSize(8.5f);
                });

                // Right Column: Authorised Signatory
                row.RelativeItem(1.0f).Column(rightCol =>
                {
                    rightCol.Item().Text($"For {Company.CompanyName}").Bold().FontSize(9f).AlignRight();
                    rightCol.Item().Height(35); // signature space
                    rightCol.Item().Text("Authorised Signatory").Bold().FontSize(9f).AlignRight();
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("E.&.O.E").FontSize(8.5f);
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(8.5f);
                x.CurrentPageNumber().FontSize(8.5f);
                x.Span(" of ").FontSize(8.5f);
                x.TotalPages().FontSize(8.5f);
            });
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
