using System;
using System.Collections.Generic;
using System.IO;
using BillingSoftware.Core.Entities;
using BillingSoftware.Printing;
using Xunit;

namespace BillingSoftware.Tests;

public class UnitTest1
{
    [Fact]
    public void TestGenerateInvoicePdf()
    {
        var company = new Company
        {
            Id = 1,
            CompanyName = "S.P.ATHIAPPAN",
            GSTIN = "33AQNPA7042F1Z6",
            PAN = "AQNPA7042F",
            Address = "6/69, MARIAMMAN KOVIL STREET, VENNANDHUR, NAMAKKAL(DT)-637505",
            Phone = "9944826732",
            Email = "spatthiappan@gmail.com",
            BankName = "KARUR VYSYA BANK LIMITED",
            BankAccountNumber = "1172135000003033",
            BankIFSC = "KVBL0001172",
            BankBranch = "VELLAIPPILLAIYAR KOVIL",
            TermsAndConditions = "1. Goods once sold will not be taken back.\n2. Subject to local jurisdiction.\n3. Interest @ 18% will be charged if payment is not made within credit days.",
            State = "Tamil Nadu"
        };

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "HEMALATHA TEXTILES",
            GSTIN = "33AEJPT8021P1ZY",
            Mobile = "-",
            State = "-",
            CreditDays = 0
        };

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = 285,
            FormattedInvoiceNumber = "285",
            InvoiceDate = new DateTime(2026, 2, 24),
            Customer = customer,
            BillingAddress = "THIRU NAGAR 4TH CROSS KAMARAJAPURAM KARUR\nTAMILNADU 639002",
            ShippingAddress = "THIRU NAGAR 4TH CROSS KAMARAJAPURAM KARUR\nTAMILNADU 639002",
            POInvoiceNumber = "0",
            PODate = new DateTime(2026, 2, 24),
            PaymentMethod = "Credit",
            SubTotal = 45000.00m,
            CGSTTotal = 1125.00m,
            SGSTTotal = 1125.00m,
            IGSTTotal = 0.00m,
            RoundOff = 0.00m,
            GrandTotal = 47250.00m
        };

        var unitPcs = new Unit { Id = Guid.NewGuid(), Code = "PCS", Description = "Pieces" };

        var items = new List<InvoiceItem>
        {
            new InvoiceItem { ProductName = "COLOUR TOWEL NA NA NA", HSNCode = "6304", Quantity = 600.00m, Rate = 51.00m, TaxableValue = 30600.00m, Product = new Product { Unit = unitPcs }, GSTPercentage = 5m, CGSTAmount = 765.00m, SGSTAmount = 765.00m, IGSTAmount = 0.00m },
            new InvoiceItem { ProductName = "FANCY STYPE NA NA NA", HSNCode = "6304", Quantity = 300.00m, Rate = 48.00m, TaxableValue = 14400.00m, Product = new Product { Unit = unitPcs }, GSTPercentage = 5m, CGSTAmount = 360.00m, SGSTAmount = 360.00m, IGSTAmount = 0.00m },
        };

        foreach (var item in items)
        {
            invoice.InvoiceItems.Add(item);
        }

        string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_invoice.pdf");
        if (File.Exists(outputPath)) File.Delete(outputPath);

        InvoicePdfGenerator.GeneratePdf(invoice, company, outputPath);

        Assert.True(File.Exists(outputPath));
        Console.WriteLine($"PDF generated successfully at: {outputPath}");
    }
}