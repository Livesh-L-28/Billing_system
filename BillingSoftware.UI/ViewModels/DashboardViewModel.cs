using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Core.Entities;
using BillingSoftware.Database;
using BillingSoftware.Application.Services;

namespace BillingSoftware.UI.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly BillingDbContext _dbContext;
    private readonly ISessionContext _sessionContext;

    // KPI Summary stats
    private int _todaySalesCount;
    public int TodaySalesCount
    {
        get => _todaySalesCount;
        set => SetProperty(ref _todaySalesCount, value);
    }

    private decimal _todaySalesTotal;
    public decimal TodaySalesTotal
    {
        get => _todaySalesTotal;
        set => SetProperty(ref _todaySalesTotal, value);
    }

    private decimal _todayGSTCollected;
    public decimal TodayGSTCollected
    {
        get => _todayGSTCollected;
        set => SetProperty(ref _todayGSTCollected, value);
    }

    private int _totalCustomers;
    public int TotalCustomers
    {
        get => _totalCustomers;
        set => SetProperty(ref _totalCustomers, value);
    }

    private int _totalProducts;
    public int TotalProducts
    {
        get => _totalProducts;
        set => SetProperty(ref _totalProducts, value);
    }

    // Low Stock warning list
    private ObservableCollection<Product> _lowStockProducts = new();
    public ObservableCollection<Product> LowStockProducts
    {
        get => _lowStockProducts;
        set => SetProperty(ref _lowStockProducts, value);
    }

    // Recent Logs list
    private ObservableCollection<AuditLog> _recentLogs = new();
    public ObservableCollection<AuditLog> RecentLogs
    {
        get => _recentLogs;
        set => SetProperty(ref _recentLogs, value);
    }

    // Refresh command
    public ICommand RefreshCommand { get; }

    public DashboardViewModel(BillingDbContext dbContext, ISessionContext sessionContext)
    {
        _dbContext = dbContext;
        _sessionContext = sessionContext;
        RefreshCommand = new AsyncRelayCommand(LoadDashboardDataAsync);

        Title = "Dashboard Summary";
        _ = LoadDashboardDataAsync();
    }

    public async Task LoadDashboardDataAsync()
    {
        IsBusy = true;
        try
        {
            DateTime todayLocal = DateTime.Today;

            // Invoices today
            Guid currentUserId = _sessionContext.CurrentUser?.Id ?? Guid.Empty;
            var todayInvoices = await _dbContext.Invoices
                .Where(i => i.InvoiceDate >= todayLocal && !i.IsCancelled && i.CreatedBy == currentUserId)
                .ToListAsync();

            TodaySalesCount = todayInvoices.Count;
            TodaySalesTotal = todayInvoices.Sum(i => i.GrandTotal);
            TodayGSTCollected = todayInvoices.Sum(i => i.CGSTTotal + i.SGSTTotal + i.IGSTTotal);

            // Customers count
            TotalCustomers = await _dbContext.Customers.CountAsync(c => c.IsActive);

            // Products count
            TotalProducts = await _dbContext.Products.CountAsync(p => p.IsActive);

            // Low Stock Products
            var lowStock = await _dbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .Where(p => p.IsActive && p.CurrentStock <= p.MinimumStockLevel)
                .OrderBy(p => p.CurrentStock)
                .Take(10)
                .ToListAsync();

            LowStockProducts = new ObservableCollection<Product>(lowStock);

            // Recent Audit Logs
            var logs = await _dbContext.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(8)
                .ToListAsync();

            RecentLogs = new ObservableCollection<AuditLog>(logs);
        }
        catch (Exception ex)
        {
            // Fail silently or print error
            System.Diagnostics.Debug.WriteLine("Dashboard error: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
