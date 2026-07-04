using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Core.Entities;

namespace BillingSoftware.Database;

public class BillingDbContext : DbContext
{
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<FinancialYear> FinancialYears { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Unit> Units { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Stock> Stocks { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<PurchaseItem> PurchaseItems { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<LoginHistory> LoginHistories { get; set; } = null!;
    public DbSet<Setting> Settings { get; set; } = null!;
    public DbSet<BackupHistory> BackupHistories { get; set; } = null!;

    public string DbPath { get; }

    public BillingDbContext()
    {
        // Default DB path in user's AppData/Local folder
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var appFolder = Path.Combine(path, "BillingSoftware");
        
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        
        DbPath = Path.Combine(appFolder, "billing.db");
    }

    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Combine(path, "BillingSoftware", "billing.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Role & User ---
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Salt).IsRequired();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);

            entity.HasOne(d => d.Role)
                .WithMany()
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Financial Year & Company ---
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.GSTIN).IsRequired().HasMaxLength(15);
            entity.Property(e => e.PAN).IsRequired().HasMaxLength(10);
            entity.Property(e => e.State).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BankBranch).IsRequired().HasDefaultValue("Main Branch");
        });

        modelBuilder.Entity<FinancialYear>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // --- Customers & Suppliers ---
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Mobile).IsRequired().HasMaxLength(15);
            entity.Property(e => e.State).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Mobile);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Name);
        });

        // --- Catalog ---
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.HSNCode).IsRequired().HasMaxLength(12);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.HSNCode);

            entity.HasOne(d => d.Category)
                .WithMany()
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Unit)
                .WithMany()
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Stock Ledger ---
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(20);

            entity.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Invoice & Items & Payments ---
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FormattedInvoiceNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.InvoiceNumber, e.FinancialYearId }).IsUnique();
            entity.HasIndex(e => e.FormattedInvoiceNumber).IsUnique();

            entity.HasOne(d => d.Customer)
                .WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.FinancialYear)
                .WithMany()
                .HasForeignKey(d => d.FinancialYearId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.HSNCode).IsRequired().HasMaxLength(12);

            entity.HasOne(d => d.Invoice)
                .WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentMode).IsRequired().HasMaxLength(30);

            entity.HasOne(d => d.Invoice)
                .WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Purchase & Items ---
        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PurchaseNumber).IsRequired().HasMaxLength(50);

            entity.HasOne(d => d.Supplier)
                .WithMany()
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.FinancialYear)
                .WithMany()
                .HasForeignKey(d => d.FinancialYearId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(d => d.Purchase)
                .WithMany(p => p.PurchaseItems)
                .HasForeignKey(d => d.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Settings & Diagnostics ---
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Single row settings
        });

        modelBuilder.Entity<BackupHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
