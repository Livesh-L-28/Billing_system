using System;
using System.Collections.Generic;
using System.IO;
using BillingSoftware.Core.Entities;
using BillingSoftware.Printing;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Database;
using System.Linq;
using System.Threading.Tasks;

namespace BillingSoftware.Tests;

public class UnitTest1
{
    [Fact]
    public void TestGenerateInvoicePdf()
    {
        var company = new Company
        {
            Id = 1,
            CompanyName = "TexVibe Textiles",
            GSTIN = "33AAAAA0000A1Z5",
            PAN = "AAAAA0000A",
            Address = "101, Textile Market, Ring Road, Erode - 638001",
            Phone = "+91 98765 43210",
            Email = "contact@texvibetextiles.com",
            BankName = "State Bank of India",
            BankAccountNumber = "123456789012",
            BankIFSC = "SBIN0001234",
            BankBranch = "Main Branch",
            TermsAndConditions = "1. Goods once sold will not be taken back.\n2. Subject to local jurisdiction.\n3. Interest @ 18% will be charged if payment is not made within credit days.",
            State = "Tamil Nadu"
        };

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Apex Garments Ltd",
            GSTIN = "33BBBBB0000B1Z6",
            Mobile = "+91 91234 56789",
            State = "Tamil Nadu",
            CreditDays = 30
        };

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = 285,
            FormattedInvoiceNumber = "INV-2026-0285",
            InvoiceDate = new DateTime(2026, 2, 24),
            Customer = customer,
            BillingAddress = "45, Commercial Street, Coimbatore\nTamil Nadu - 641001",
            ShippingAddress = "45, Commercial Street, Coimbatore\nTamil Nadu - 641001",
            POInvoiceNumber = "PO-ABC123XYZ",
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
            new InvoiceItem { ProductName = "Cotton Bath Towel (Standard)", HSNCode = "6304", Quantity = 600.00m, Rate = 51.00m, TaxableValue = 30600.00m, Product = new Product { Unit = unitPcs }, GSTPercentage = 5m, CGSTAmount = 765.00m, SGSTAmount = 765.00m, IGSTAmount = 0.00m },
            new InvoiceItem { ProductName = "Fancy Jacquard Towel", HSNCode = "6304", Quantity = 300.00m, Rate = 48.00m, TaxableValue = 14400.00m, Product = new Product { Unit = unitPcs }, GSTPercentage = 5m, CGSTAmount = 360.00m, SGSTAmount = 360.00m, IGSTAmount = 0.00m },
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

    [Fact]
    public void TestGenerateInvoicePdfForServices()
    {
        var company = new Company
        {
            Id = 1,
            CompanyName = "TexVibe Textiles",
            GSTIN = "33AAAAA0000A1Z5",
            PAN = "AAAAA0000A",
            Address = "101, Textile Market, Ring Road, Erode - 638001",
            Phone = "+91 98765 43210",
            Email = "contact@texvibetextiles.com",
            BankName = "State Bank of India",
            BankAccountNumber = "123456789012",
            BankIFSC = "SBIN0001234",
            BankBranch = "Main Branch",
            TermsAndConditions = "1. Goods once sold will not be taken back.\n2. Subject to local jurisdiction.\n3. Interest @ 18% will be charged if payment is not made within credit days.",
            State = "Tamil Nadu"
        };

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Apex Garments Ltd",
            GSTIN = "33BBBBB0000B1Z6",
            Mobile = "+91 91234 56789",
            State = "Tamil Nadu",
            CreditDays = 30
        };

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = 286,
            FormattedInvoiceNumber = "INV-2026-0286",
            InvoiceDate = new DateTime(2026, 2, 24),
            Customer = customer,
            BillingAddress = "45, Commercial Street, Coimbatore\nTamil Nadu - 641001",
            ShippingAddress = "45, Commercial Street, Coimbatore\nTamil Nadu - 641001",
            POInvoiceNumber = "PO-ABC123XYZ",
            PODate = new DateTime(2026, 2, 24),
            PaymentMethod = "Credit",
            SubTotal = 15000.00m,
            CGSTTotal = 375.00m,
            SGSTTotal = 375.00m,
            IGSTTotal = 0.00m,
            RoundOff = 0.00m,
            GrandTotal = 15750.00m
        };

        var unitPcs = new Unit { Id = Guid.NewGuid(), Code = "pcs", Description = "Pieces" };

        var items = new List<InvoiceItem>
        {
            new InvoiceItem { ProductName = "stitching service charges", HSNCode = "9988", Quantity = 1.00m, Rate = 15000.00m, TaxableValue = 15000.00m, Product = new Product { Unit = unitPcs }, GSTPercentage = 5m, CGSTAmount = 375.00m, SGSTAmount = 375.00m, IGSTAmount = 0.00m },
        };

        foreach (var item in items)
        {
            invoice.InvoiceItems.Add(item);
        }

        string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_invoice_services.pdf");
        if (File.Exists(outputPath)) File.Delete(outputPath);

        InvoicePdfGenerator.GeneratePdf(invoice, company, outputPath);

        Assert.True(File.Exists(outputPath));
        Console.WriteLine($"PDF for services generated successfully at: {outputPath}");
    }

    [Fact]
    public void TestDatabaseDiag()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BillingSoftware", "billing.db");
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_diag.txt");
        using (var sw = new StreamWriter(logPath))
        {
            sw.WriteLine($"DB Path: {dbPath}");
            sw.WriteLine($"Exists: {File.Exists(dbPath)}");
            
            if (File.Exists(dbPath))
            {
                var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<BillingSoftware.Database.BillingDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;
                    
                using (var db = new BillingSoftware.Database.BillingDbContext(options))
                {
                    BillingSoftware.Database.DbInitializer.Initialize(db);
                    
                    sw.WriteLine("--- Users ---");
                    foreach (var u in db.Users)
                    {
                        sw.WriteLine($"User: {u.Username}, FullName: {u.FullName}, RoleId: {u.RoleId}");
                    }
                    
                    sw.WriteLine("--- Companies ---");
                    foreach (var c in db.Companies)
                    {
                        sw.WriteLine($"Company: {c.Id}, Name: {c.CompanyName}, GSTIN: '{c.GSTIN}', State: '{c.State}'");
                    }
                    
                    sw.WriteLine("--- Products ---");
                    foreach (var p in db.Products)
                    {
                        sw.WriteLine($"Product: {p.Id}, Name: '{p.Name}', HSN: '{p.HSNCode}', GST%: {p.GSTPercentage}, Price: {p.SalesPrice}");
                    }

                    sw.WriteLine("--- Latest Invoices ---");
                    var latestInvoices = db.Invoices
                        .Include(i => i.Customer)
                        .OrderByDescending(i => i.InvoiceDate)
                        .Take(5)
                        .ToList();
                    foreach (var inv in latestInvoices)
                    {
                        sw.WriteLine($"Invoice: {inv.InvoiceNumber}, Date: {inv.InvoiceDate:yyyy-MM-dd}, Customer: '{inv.Customer?.Name}', CGSTTotal: {inv.CGSTTotal}, SGSTTotal: {inv.SGSTTotal}, IGSTTotal: {inv.IGSTTotal}, SubTotal: {inv.SubTotal}, GrandTotal: {inv.GrandTotal}");
                    }
                }
            }
        }
        Assert.True(true);
    }

    [Fact]
    public void TestIsIntraState()
    {
        // 1. Same GSTIN prefix (State codes)
        Assert.True(BillingSoftware.UI.ViewModels.BillingViewModel.IsIntraState("Maharashtra", "27AAACT1111A1Z1", "Tamil Nadu", "27BBBDU2222B2Z2"));
        Assert.True(BillingSoftware.UI.ViewModels.BillingViewModel.IsIntraState("Tamil Nadu", "33AQNPA7042F1Z6", "Tamil Nadu", "33AJTPP8862B1ZT"));

        // 2. Different GSTIN prefix
        Assert.False(BillingSoftware.UI.ViewModels.BillingViewModel.IsIntraState("Tamil Nadu", "33AQNPA7042F1Z6", "Maharashtra", "27BBBDU2222B2Z2"));

        // 3. One or both missing GSTIN, fallback to normalized state names
        Assert.True(BillingSoftware.UI.ViewModels.BillingViewModel.IsIntraState("Tamil Nadu", "", "Tamil Nadu", ""));
        Assert.True(BillingSoftware.UI.ViewModels.BillingViewModel.IsIntraState("Tamil Nadu", null, "Tamilnadu", null));
        Assert.True(BillingSoftware.UI.ViewModels.BillingViewModel.IsIntraState("TN", "   ", "Tamil Nadu", ""));
        Assert.False(BillingSoftware.UI.ViewModels.BillingViewModel.IsIntraState("Maharashtra", "", "Tamil Nadu", ""));
    }

    [Fact]
    public void TestInvoiceStoragePathLogic()
    {
        string username1 = "samundeeswari";
        string userFolder1 = username1.Contains("samundeeswari") ? "samundeeswari" : "pathiappan";
        Assert.Equal("samundeeswari", userFolder1);

        string username2 = "samundeeswari_admin";
        string userFolder2 = username2.Contains("samundeeswari") ? "samundeeswari" : "pathiappan";
        Assert.Equal("samundeeswari", userFolder2);

        string username3 = "pathiappan";
        string userFolder3 = username3.Contains("samundeeswari") ? "samundeeswari" : "pathiappan";
        Assert.Equal("pathiappan", userFolder3);

        string username4 = "manager";
        string userFolder4 = username4.Contains("samundeeswari") ? "samundeeswari" : "pathiappan";
        Assert.Equal("pathiappan", userFolder4);
    }

    [Fact]
    public async Task TestEditInvoiceConcurrency()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseSqlite("Data Source=test_concurrency.db")
            .Options;

        using (var dbContext = new BillingDbContext(options))
        {
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            // Seed minimal items
            var activeFY = new FinancialYear { Id = Guid.NewGuid(), Name = "2026-27", IsLocked = false };
            await dbContext.FinancialYears.AddAsync(activeFY);

            var cat = new Category { Id = Guid.NewGuid(), Name = "TOWEL" };
            await dbContext.Categories.AddAsync(cat);

            var unit = new Unit { Id = Guid.NewGuid(), Code = "PCS", Description = "Pieces" };
            await dbContext.Units.AddAsync(unit);

            var customer = new Customer { Id = Guid.NewGuid(), Name = "Test Customer", Mobile = "9999999999", State = "Tamil Nadu" };
            await dbContext.Customers.AddAsync(customer);

            var product = new Product { Id = Guid.NewGuid(), Name = "Test Product", HSNCode = "1234", CategoryId = cat.Id, UnitId = unit.Id, CurrentStock = 100, IsActive = true };
            await dbContext.Products.AddAsync(product);

            await dbContext.SaveChangesAsync();

            // Create Invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = 1,
                FormattedInvoiceNumber = "INV-2026-0001",
                CustomerId = customer.Id,
                FinancialYearId = activeFY.Id,
                PaymentMethod = "Credit",
                GrandTotal = 100
            };

            var invoiceItem = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                HSNCode = product.HSNCode,
                Quantity = 10,
                Rate = 10,
                TotalAmount = 100
            };
            invoice.InvoiceItems.Add(invoiceItem);

            await dbContext.Invoices.AddAsync(invoice);
            await dbContext.SaveChangesAsync();
        }

        // Now edit it in another context (or same, mimicking the app context)
        using (var dbContext = new BillingDbContext(options))
        {
            var invoiceId = dbContext.Invoices.First().Id;

            // Mimic LoadInvoiceForEditingAsync
            var editInvoice = await dbContext.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            // Clear items like UI list
            var itemsList = editInvoice!.InvoiceItems.ToList();

            // SaveInvoiceInternalAsync logic
            var invoiceToSave = await dbContext.Invoices
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            // Revert stock (mimic)
            foreach (var oldItem in invoiceToSave!.InvoiceItems)
            {
                var oldProduct = await dbContext.Products.FindAsync(oldItem.ProductId);
                if (oldProduct != null)
                {
                    oldProduct.CurrentStock += oldItem.Quantity;
                }
            }

            // Remove old items
            var oldItems = invoiceToSave.InvoiceItems.ToList();
            if (oldItems.Any())
            {
                dbContext.InvoiceItems.RemoveRange(oldItems);
                invoiceToSave.InvoiceItems.Clear();
            }

            // Copy items to invoice
            foreach (var uiItem in itemsList)
            {
                var newItem = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoiceToSave.Id,
                    ProductId = uiItem.ProductId,
                    ProductName = uiItem.ProductName,
                    HSNCode = uiItem.HSNCode,
                    Quantity = 5, // modify quantity
                    Rate = 10,
                    TotalAmount = 50
                };
                invoiceToSave.InvoiceItems.Add(newItem);
                dbContext.Entry(newItem).State = EntityState.Added;
            }

            // Save
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var details = string.Join(", ", ex.Entries.Select(e => $"{e.Entity.GetType().Name} (State: {e.State})"));
                throw new Exception($"Concurrency details: {details}", ex);
            }
        }
    }
}