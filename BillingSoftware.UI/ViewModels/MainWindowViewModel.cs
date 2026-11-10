using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using BillingSoftware.Application.Services;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Database;
using System.Linq;

namespace BillingSoftware.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ISessionContext _sessionContext;
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;

    private ViewModelBase? _currentViewModel;
    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    private bool _isSidebarExpanded = true;
    public bool IsSidebarExpanded
    {
        get => _isSidebarExpanded;
        set => SetProperty(ref _isSidebarExpanded, value);
    }

    public string CurrentUserDisplayName => _sessionContext.CurrentUser?.FullName ?? "User";
    public string CurrentUserRole => _sessionContext.CurrentUser?.Role?.RoleName ?? "Role";
    public string CurrentFinancialYearName => _sessionContext.CurrentFinancialYear?.Name ?? "2026-27";

    public ICommand ToggleSidebarCommand { get; }
    public ICommand ShowDashboardCommand { get; }
    public ICommand ShowCustomersCommand { get; }
    public ICommand ShowProductsCommand { get; }
    public ICommand ShowSuppliersCommand { get; }
    public ICommand ShowBillingCommand { get; }
    public ICommand ShowReportsCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand ShowBackupCommand { get; }
    public ICommand LogoutCommand { get; }

    // public event Action? RequestClose;
    public event Action? RequestLoginRedirect;

    public MainWindowViewModel(
        ISessionContext sessionContext,
        IAuthService authService,
        IServiceProvider serviceProvider)
    {
        _sessionContext = sessionContext;
        _authService = authService;
        _serviceProvider = serviceProvider;

        ToggleSidebarCommand = new RelayCommand(() => IsSidebarExpanded = !IsSidebarExpanded);
        ShowDashboardCommand = new RelayCommand(ShowDashboard);
        ShowCustomersCommand = new RelayCommand(ShowCustomers);
        ShowProductsCommand = new RelayCommand(ShowProducts);
        ShowSuppliersCommand = new RelayCommand(ShowSuppliers);
        ShowBillingCommand = new RelayCommand(ShowBilling);
        ShowReportsCommand = new RelayCommand(ShowReports);
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        ShowBackupCommand = new RelayCommand(ShowBackup);
        LogoutCommand = new AsyncRelayCommand(ExecuteLogoutAsync);

        Title = "Bill Soft GST Billing";

        // Initial view
        ShowDashboard();

        _sessionContext.NavigationRequested += async (viewName, param) =>
        {
            if (viewName == "BillingViewModel")
            {
                if (HasPermission("Billing"))
                {
                    var billingVm = ResolveViewModel<BillingViewModel>();
                    CurrentViewModel = billingVm;
                    if (param is string invoiceNo)
                    {
                        await billingVm.LoadInvoiceForEditingAsync(invoiceNo);
                    }
                }
            }
        };
    }

    private void ShowDashboard()
    {
        CurrentViewModel = ResolveViewModel<DashboardViewModel>();
    }

    private void ShowCustomers()
    {
        if (HasPermission("Customer"))
        {
            CurrentViewModel = ResolveViewModel<CustomerListViewModel>();
        }
    }

    private void ShowProducts()
    {
        if (HasPermission("Product"))
        {
            CurrentViewModel = ResolveViewModel<ProductListViewModel>();
        }
    }

    private void ShowSuppliers()
    {
        if (HasPermission("Product")) // Suppliers access falls under master details
        {
            CurrentViewModel = ResolveViewModel<SupplierListViewModel>();
        }
    }

    private void ShowBilling()
    {
        if (HasPermission("Billing"))
        {
            CurrentViewModel = ResolveViewModel<BillingViewModel>();
        }
    }

    private void ShowReports()
    {
        if (HasPermission("Reports"))
        {
            CurrentViewModel = ResolveViewModel<ReportsViewModel>();
        }
    }

    private void ShowSettings()
    {
        if (HasPermission("All")) // Settings are restricted to Admin
        {
            CurrentViewModel = ResolveViewModel<SettingsViewModel>();
        }
    }

    private void ShowBackup()
    {
        if (HasPermission("Backup"))
        {
            CurrentViewModel = ResolveViewModel<BackupViewModel>();
        }
    }

    private async Task ExecuteLogoutAsync()
    {
        try
        {
            // Auto-backup on logout
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var settings = dbContext.Settings.FirstOrDefault(s => s.Id == _sessionContext.CurrentSettingId);
            string backupDir = settings?.BackupFolderPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(backupDir))
            {
                backupDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BillingSoftware", "Backups");
            }

            var connectionString = dbContext.Database.GetDbConnection().ConnectionString;
            string dbFilePath = connectionString.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();

            string fileName = $"Auto_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            string destZipPath = System.IO.Path.Combine(backupDir, fileName);

            var backupService = new BillingSoftware.Backup.BackupService();
            await Task.Run(() => backupService.CreateBackup(dbFilePath, destZipPath));
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Automated backup on logout failed.");
        }

        await _authService.LogoutAsync();
        RequestLoginRedirect?.Invoke();
    }

    private bool HasPermission(string permission)
    {
        if (_authService.HasPermission(permission))
        {
            return true;
        }

        System.Windows.MessageBox.Show(
            $"You do not have permission to access the {permission} module.", 
            "Access Denied", 
            System.Windows.MessageBoxButton.OK, 
            System.Windows.MessageBoxImage.Warning);
        return false;
    }

    private T ResolveViewModel<T>() where T : ViewModelBase
    {
        var vm = _serviceProvider.GetRequiredService<T>();
        return vm;
    }
}
