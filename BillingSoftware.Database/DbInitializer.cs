using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Core.Entities;
using BillingSoftware.Common;

namespace BillingSoftware.Database;

public static class DbInitializer
{
    public static void Initialize(BillingDbContext context)
    {
        // 1. Run migrations
        context.Database.Migrate();

        // 1b. Drop unique constraint index on InvoiceNumber + FinancialYearId to allow separate sequence numbering
        try
        {
            context.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS IX_Invoices_InvoiceNumber_FinancialYearId;");
            context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Invoices_InvoiceNumber_FinancialYearId ON Invoices (InvoiceNumber, FinancialYearId);");
        }
        catch (Exception)
        {
            // Index modification fallback
        }

        // 1c. Add InvoiceStorageFolderPath column if it doesn't exist
        try
        {
            context.Database.ExecuteSqlRaw("ALTER TABLE Settings ADD COLUMN InvoiceStorageFolderPath TEXT;");
        }
        catch (Exception)
        {
            // Column already exists or table doesn't exist yet
        }

        // Fill NULL values in InvoiceStorageFolderPath using Raw SQL to prevent EF Core query crashes
        try
        {
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BillingSoftware", "Invoices");
            context.Database.ExecuteSqlRaw("UPDATE Settings SET InvoiceStorageFolderPath = {0} WHERE InvoiceStorageFolderPath IS NULL;", defaultPath);
        }
        catch (Exception)
        {
            // Ignore database errors
        }

        // 2. Seed Roles
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { Id = 1, RoleName = "Admin", Permissions = "[\"All\"]" },
                new Role { Id = 2, RoleName = "Manager", Permissions = "[\"Billing\", \"Customer\", \"Product\", \"GST\", \"Reports\", \"Backup\"]" },
                new Role { Id = 3, RoleName = "Cashier", Permissions = "[\"Billing\", \"Customer\", \"Product\"]" },
                new Role { Id = 4, RoleName = "Staff", Permissions = "[\"Customer\", \"Product\"]" }
            );
            context.SaveChanges();
        }

        // 3. Seed Users
        if (!context.Users.Any())
        {
            string adminHash = PasswordHasher.HashPassword("admin123", out string adminSalt);
            string managerHash = PasswordHasher.HashPassword("manager123", out string managerSalt);
            string cashierHash = PasswordHasher.HashPassword("cashier123", out string cashierSalt);
            string staffHash = PasswordHasher.HashPassword("staff123", out string staffSalt);

            context.Users.AddRange(
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    PasswordHash = adminHash,
                    Salt = adminSalt,
                    FullName = "System Administrator",
                    RoleId = 1,
                    CompanyId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "manager",
                    PasswordHash = managerHash,
                    Salt = managerSalt,
                    FullName = "Store Manager",
                    RoleId = 2,
                    CompanyId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "cashier",
                    PasswordHash = cashierHash,
                    Salt = cashierSalt,
                    FullName = "Cash Counter 1",
                    RoleId = 3,
                    CompanyId = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "staff",
                    PasswordHash = staffHash,
                    Salt = staffSalt,
                    FullName = "Sales Assistant",
                    RoleId = 4,
                    CompanyId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
            context.SaveChanges();
        }

        // 3b. Ensure "admin" and "cashier" users exist for terminal login
        var oldAdmin = context.Users.FirstOrDefault(u => u.Username == "admin");
        var pathiappanUser = context.Users.FirstOrDefault(u => u.Username == "pathiappan");
        string pHash = PasswordHasher.HashPassword("admin123", out string pSalt);
        if (oldAdmin == null)
        {
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                PasswordHash = pHash,
                Salt = pSalt,
                FullName = "System Administrator",
                RoleId = 1,
                CompanyId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }
        else
        {
            oldAdmin.FullName = "System Administrator";
            oldAdmin.PasswordHash = pHash;
            oldAdmin.Salt = pSalt;
            context.Users.Update(oldAdmin);
            context.SaveChanges();
        }

        if (pathiappanUser != null)
        {
            pathiappanUser.FullName = "System Administrator";
            pathiappanUser.PasswordHash = pHash;
            pathiappanUser.Salt = pSalt;
            context.Users.Update(pathiappanUser);
            context.SaveChanges();
        }

        var oldCashier = context.Users.FirstOrDefault(u => u.Username == "cashier");
        var samundeeswariUser = context.Users.FirstOrDefault(u => u.Username == "samundeeswari");
        string sHash = PasswordHasher.HashPassword("admin123", out string sSalt);
        if (oldCashier == null)
        {
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = "cashier",
                PasswordHash = sHash,
                Salt = sSalt,
                FullName = "Cashier Terminal",
                RoleId = 1,
                CompanyId = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }
        else
        {
            oldCashier.FullName = "Cashier Terminal";
            oldCashier.RoleId = 1;
            oldCashier.PasswordHash = sHash;
            oldCashier.Salt = sSalt;
            context.Users.Update(oldCashier);
            context.SaveChanges();
        }

        if (samundeeswariUser != null)
        {
            samundeeswariUser.FullName = "Cashier Terminal";
            samundeeswariUser.RoleId = 1;
            samundeeswariUser.PasswordHash = sHash;
            samundeeswariUser.Salt = sSalt;
            context.Users.Update(samundeeswariUser);
            context.SaveChanges();
        }

        // 4. Seed Default Companies (TexVibe Textiles - ID 1, Vibe Weaves - ID 2)
        if (!context.Companies.Any(c => c.Id == 1))
        {
            context.Companies.Add(new Company
            {
                Id = 1,
                CompanyName = "TEXVIBE TEXTILES",
                GSTIN = "33AAAAA0000A1Z5",
                PAN = "AAAAA0000A",
                Address = "101, Textile Market, Ring Road, Erode - 638001",
                Phone = "+91 98765 43210",
                Email = "contact@texvibetextiles.com",
                BankName = "State Bank of India",
                BankAccountNumber = "123456789012",
                BankIFSC = "SBIN0001234",
                State = "Tamil Nadu",
                TermsAndConditions = "1. Goods once sold will not be taken back.\n2. Subject to Local Jurisdiction only.\n3. Interest @ 18% will be charged if payment is not made within credit days."
            });
            context.SaveChanges();
        }

        if (!context.Companies.Any(c => c.Id == 2))
        {
            context.Companies.Add(new Company
            {
                Id = 2,
                CompanyName = "VIBE WEAVES",
                GSTIN = "33BBBBB0000B1Z6",
                PAN = "BBBBB0000B",
                Address = "202, Weaver Colony, Coimbatore - 641001",
                Phone = "+91 98765 00001",
                Email = "info@vibeweaves.com",
                BankName = "Indian Bank",
                BankAccountNumber = "987654321098",
                BankIFSC = "IDIB0005678",
                State = "Tamil Nadu",
                TermsAndConditions = "1. Goods once sold will not be taken back.\n2. Subject to Local Jurisdiction only."
            });
            context.SaveChanges();
        }

        // 5. Seed Financial Years
        if (!context.FinancialYears.Any())
        {
            context.FinancialYears.AddRange(
                new FinancialYear
                {
                    Id = Guid.NewGuid(),
                    Name = "2026-27",
                    StartDate = new DateTime(2026, 4, 1),
                    EndDate = new DateTime(2027, 3, 31),
                    IsActive = true,
                    IsLocked = false
                },
                new FinancialYear
                {
                    Id = Guid.NewGuid(),
                    Name = "2027-28",
                    StartDate = new DateTime(2027, 4, 1),
                    EndDate = new DateTime(2028, 3, 31),
                    IsActive = false,
                    IsLocked = false
                }
            );
            context.SaveChanges();
        }

        // 6. Seed Settings (TexVibe Textiles - ID 1, Vibe Weaves - ID 2)
        if (!context.Settings.Any(s => s.Id == 1))
        {
            context.Settings.Add(new Setting
            {
                Id = 1,
                InvoicePrefix = "TXV",
                InvoiceFormat = "{Prefix}-{Year}-{Number:D4}",
                EnableRoundOff = true,
                Theme = "Light",
                BackupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BillingSoftware", "Backups"),
                DefaultPrinterName = string.Empty,
                InvoiceStorageFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BillingSoftware", "Invoices")
            });
            context.SaveChanges();
        }

        if (!context.Settings.Any(s => s.Id == 2))
        {
            context.Settings.Add(new Setting
            {
                Id = 2,
                InvoicePrefix = "VBW",
                InvoiceFormat = "{Prefix}-{Year}-{Number:D4}",
                EnableRoundOff = true,
                Theme = "Dark",
                BackupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BillingSoftware", "Backups"),
                DefaultPrinterName = string.Empty,
                InvoiceStorageFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BillingSoftware", "Invoices")
            });
            context.SaveChanges();
        }

        // Initialize empty InvoiceStorageFolderPath for existing Settings
        var existingSettings = context.Settings.ToList();
        foreach (var s in existingSettings)
        {
            if (string.IsNullOrWhiteSpace(s.InvoiceStorageFolderPath))
            {
                s.InvoiceStorageFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BillingSoftware", "Invoices");
                context.Settings.Update(s);
            }
        }
        context.SaveChanges();

        // 7. Seed Units
        if (!context.Units.Any())
        {
            context.Units.AddRange(
                new Unit { Id = Guid.NewGuid(), Code = "PCS", Description = "Pieces" },
                new Unit { Id = Guid.NewGuid(), Code = "MTR", Description = "Meters" },
                new Unit { Id = Guid.NewGuid(), Code = "KGS", Description = "Kilograms" },
                new Unit { Id = Guid.NewGuid(), Code = "BOX", Description = "Boxes" }
            );
            context.SaveChanges();
        }

        // 8. Seed/Maintain Categories (only TOWEL and RAW MATERIAL)
        var towelCat = context.Categories.FirstOrDefault(c => c.Name == "TOWEL");
        if (towelCat == null)
        {
            towelCat = new Category { Id = Guid.NewGuid(), Name = "TOWEL", Description = "Towel category" };
            context.Categories.Add(towelCat);
            context.SaveChanges();
        }

        var rawMaterialCat = context.Categories.FirstOrDefault(c => c.Name == "raw material" || c.Name == "RAW MATERIAL");
        if (rawMaterialCat == null)
        {
            rawMaterialCat = new Category { Id = Guid.NewGuid(), Name = "RAW MATERIAL", Description = "Raw material category" };
            context.Categories.Add(rawMaterialCat);
            context.SaveChanges();
        }
        else if (rawMaterialCat.Name != "RAW MATERIAL")
        {
            rawMaterialCat.Name = "RAW MATERIAL";
            context.Categories.Update(rawMaterialCat);
            context.SaveChanges();
        }

        // Clean up any other categories to ensure ONLY these two are present
        var otherCats = context.Categories.Where(c => c.Name != "TOWEL" && c.Name != "RAW MATERIAL").ToList();
        if (otherCats.Any())
        {
            // Point any existing products with other categories to TOWEL
            var otherCatIds = otherCats.Select(c => c.Id).ToList();
            var productsToUpdate = context.Products.Where(p => otherCatIds.Contains(p.CategoryId)).ToList();
            foreach (var p in productsToUpdate)
            {
                p.CategoryId = towelCat.Id;
                context.Products.Update(p);
            }
            context.SaveChanges();

            // Now safely remove the old categories
            context.Categories.RemoveRange(otherCats);
            context.SaveChanges();
        }

        // 9. Run database cleanup/fixes for existing data & sanitize showcase details
        var existingCompany1 = context.Companies.FirstOrDefault(c => c.Id == 1);
        if (existingCompany1 != null)
        {
            existingCompany1.CompanyName = "TEXVIBE TEXTILES";
            existingCompany1.GSTIN = "33AAAAA0000A1Z5";
            existingCompany1.PAN = "AAAAA0000A";
            existingCompany1.Address = "101, Textile Market, Ring Road, Erode - 638001";
            existingCompany1.Phone = "+91 98765 43210";
            existingCompany1.Email = "contact@texvibetextiles.com";
            existingCompany1.BankName = "State Bank of India";
            existingCompany1.BankAccountNumber = "123456789012";
            existingCompany1.BankIFSC = "SBIN0001234";
            existingCompany1.State = "Tamil Nadu";
            existingCompany1.TermsAndConditions = "1. Goods once sold will not be taken back.\n2. Subject to Local Jurisdiction only.\n3. Interest @ 18% will be charged if payment is not made within credit days.";
            context.Companies.Update(existingCompany1);
            context.SaveChanges();
        }

        var existingCompany2 = context.Companies.FirstOrDefault(c => c.Id == 2);
        if (existingCompany2 != null)
        {
            existingCompany2.CompanyName = "VIBE WEAVES";
            existingCompany2.GSTIN = "33BBBBB0000B1Z6";
            existingCompany2.PAN = "BBBBB0000B";
            existingCompany2.Address = "202, Weaver Colony, Coimbatore - 641001";
            existingCompany2.Phone = "+91 98765 00001";
            existingCompany2.Email = "info@vibeweaves.com";
            existingCompany2.BankName = "Indian Bank";
            existingCompany2.BankAccountNumber = "987654321098";
            existingCompany2.BankIFSC = "IDIB0005678";
            existingCompany2.State = "Tamil Nadu";
            existingCompany2.TermsAndConditions = "1. Goods once sold will not be taken back.\n2. Subject to Local Jurisdiction only.";
            context.Companies.Update(existingCompany2);
            context.SaveChanges();
        }

        var existingSetting1 = context.Settings.FirstOrDefault(s => s.Id == 1);
        if (existingSetting1 != null && (existingSetting1.InvoicePrefix == "SPA" || string.IsNullOrWhiteSpace(existingSetting1.InvoicePrefix)))
        {
            existingSetting1.InvoicePrefix = "TXV";
            context.Settings.Update(existingSetting1);
            context.SaveChanges();
        }

        var existingSetting2 = context.Settings.FirstOrDefault(s => s.Id == 2);
        if (existingSetting2 != null && (existingSetting2.InvoicePrefix == "ASM" || string.IsNullOrWhiteSpace(existingSetting2.InvoicePrefix)))
        {
            existingSetting2.InvoicePrefix = "VBW";
            context.Settings.Update(existingSetting2);
            context.SaveChanges();
        }

        // Sanitize existing customer showcase details
        var dineshCust = context.Customers.FirstOrDefault(c => c.Name == "DINESH" || c.GSTIN == "33AEJPT8021P1ZY");
        if (dineshCust != null)
        {
            dineshCust.Name = "APEX GARMENTS LTD";
            dineshCust.GSTIN = "33CCCCC0000C1Z7";
            dineshCust.PAN = "CCCCC0000C";
            dineshCust.Address = "12, Mill Road, Industrial Area, Karur - 639002";
            dineshCust.Mobile = "+91 98765 11111";
            context.Customers.Update(dineshCust);
        }

        var sivaramCust = context.Customers.FirstOrDefault(c => c.Name == "SIVARAM STORES" || c.GSTIN == "33AJTPP8862B1ZT");
        if (sivaramCust != null)
        {
            sivaramCust.Name = "SUNRISE RETAILERS";
            sivaramCust.GSTIN = "33DDDDD0000D1Z8";
            sivaramCust.PAN = "DDDDD0000D";
            sivaramCust.Address = "45, Commercial Street, Erode - 638001";
            sivaramCust.Mobile = "+91 98765 22222";
            context.Customers.Update(sivaramCust);
        }

        var sudharsanCust = context.Customers.FirstOrDefault(c => c.Name == "SUDHARSAN TEXTILES" || c.GSTIN == "33ATLPS4313A1Z2");
        if (sudharsanCust != null)
        {
            sudharsanCust.Name = "GLOBAL APPAREL CORP";
            sudharsanCust.GSTIN = "33EEEEE0000E1Z9";
            sudharsanCust.PAN = "EEEEE0000E";
            sudharsanCust.Address = "88, Market Street, Erode - 638001";
            sudharsanCust.Mobile = "+91 98765 33333";
            context.Customers.Update(sudharsanCust);
        }
        context.SaveChanges();

        // Sanitize any addresses in past invoices
        var oldInvoices = context.Invoices.Where(i => 
            (i.BillingAddress != null && (i.BillingAddress.Contains("MARIAMMAN") || i.BillingAddress.Contains("VENNANDHUR"))) ||
            (i.ShippingAddress != null && (i.ShippingAddress.Contains("MARIAMMAN") || i.ShippingAddress.Contains("VENNANDHUR")))).ToList();
        foreach (var inv in oldInvoices)
        {
            inv.BillingAddress = "101, Textile Market, Ring Road, Erode - 638001";
            inv.ShippingAddress = "101, Textile Market, Ring Road, Erode - 638001";
            context.Invoices.Update(inv);
        }
        if (oldInvoices.Any())
        {
            context.SaveChanges();
        }

        // Clean up leading/trailing spaces for all companies and customers
        var companies = context.Companies.ToList();
        foreach (var c in companies)
        {
            string trimmedName = c.CompanyName?.Trim() ?? "";
            string trimmedGstin = c.GSTIN?.Trim() ?? "";
            string trimmedPan = c.PAN?.Trim() ?? "";
            if (c.CompanyName != trimmedName || c.GSTIN != trimmedGstin || c.PAN != trimmedPan)
            {
                c.CompanyName = trimmedName;
                c.GSTIN = trimmedGstin;
                c.PAN = trimmedPan;
                context.Companies.Update(c);
            }
        }

        var customers = context.Customers.ToList();
        foreach (var cust in customers)
        {
            string trimmedName = cust.Name?.Trim() ?? "";
            string trimmedGstin = cust.GSTIN?.Trim() ?? "";
            string trimmedPan = cust.PAN?.Trim() ?? "";
            if (cust.Name != trimmedName || cust.GSTIN != trimmedGstin || cust.PAN != trimmedPan)
            {
                cust.Name = trimmedName;
                cust.GSTIN = trimmedGstin;
                cust.PAN = trimmedPan;
                context.Customers.Update(cust);
            }
        }
        context.SaveChanges();

        // Correct product GST percentages for COLOUR TOWEL and FANCY STYPE (which should be 5%)
        var products = context.Products.ToList();
        foreach (var p in products)
        {
            string name = p.Name?.Trim() ?? "";
            string hsn = p.HSNCode?.Trim() ?? "";
            
            // Trim product fields
            if (p.Name != name || p.HSNCode != hsn)
            {
                p.Name = name;
                p.HSNCode = hsn;
                context.Products.Update(p);
            }

            if (name.Contains("TOWEL", StringComparison.OrdinalIgnoreCase) || 
                name.Contains("STYPE", StringComparison.OrdinalIgnoreCase) || 
                hsn == "6304")
            {
                if (p.GSTPercentage != 5)
                {
                    p.GSTPercentage = 5;
                    context.Products.Update(p);
                }
            }
        }
        context.SaveChanges();
    }
}
