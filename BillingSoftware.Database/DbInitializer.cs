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
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
            context.SaveChanges();
        }

        // 4. Seed Default Company
        if (!context.Companies.Any())
        {
            context.Companies.Add(new Company
            {
                Id = 1,
                CompanyName = "TexVibe Apparel Inc",
                GSTIN = "27AAACT1111A1Z1",
                PAN = "AAACT1111A",
                Address = "101, Textile Market, Ring Road",
                Phone = "+91 98765 43210",
                Email = "info@texvibe.com",
                BankName = "State Bank of India",
                BankAccountNumber = "123456789012",
                BankIFSC = "SBIN0001234",
                State = "Maharashtra",
                TermsAndConditions = "1. Goods once sold will not be taken back.\n2. Subject to local jurisdiction.\n3. Interest @ 18% will be charged if payment is not made within credit days."
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

        // 6. Seed Settings
        if (!context.Settings.Any())
        {
            context.Settings.Add(new Setting
            {
                Id = 1,
                InvoicePrefix = "TXV",
                InvoiceFormat = "{Prefix}-{Year}-{Number:D4}",
                EnableRoundOff = true,
                Theme = "Light",
                BackupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BillingSoftware", "Backups"),
                DefaultPrinterName = string.Empty
            });
            context.SaveChanges();
        }

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

        // 8. Seed Categories
        if (!context.Categories.Any())
        {
            context.Categories.AddRange(
                new Category { Id = Guid.NewGuid(), Name = "Sarees", Description = "Designer and silk sarees" },
                new Category { Id = Guid.NewGuid(), Name = "Suitings", Description = "Men's suit fabrics" },
                new Category { Id = Guid.NewGuid(), Name = "Shirts", Description = "Readymade shirts" },
                new Category { Id = Guid.NewGuid(), Name = "Yarn", Description = "Weaving yarns" }
            );
            context.SaveChanges();
        }
    }
}
