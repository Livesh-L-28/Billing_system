using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using BillingSoftware.Core.Repositories;
using BillingSoftware.Database;
using BillingSoftware.Application.Services;
using BillingSoftware.Infrastructure.Repositories;
using BillingSoftware.Infrastructure.Services;
using BillingSoftware.Infrastructure.Logging;
using BillingSoftware.UI.ViewModels;

namespace BillingSoftware.UI;

public partial class App : System.Windows.Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Initialize logging
        LoggerConfig.Initialize();
        Log.Information("Application starting up...");

        try
        {
            // 2. Setup Dependency Injection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // 3. Initialize/Migrate Database
            Log.Information("Initializing SQLite Database...");
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
                DbInitializer.Initialize(dbContext);
            }
            Log.Information("Database initialized successfully.");

            // 4. Set Default Theme from Settings
            ApplySavedTheme();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed.");
            MessageBox.Show($"Application failed to start:\n{ex.Message}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // 5. Open Login Selection Window
        Log.Information("Showing Login Selection Window...");
        var selectionWindow = new LoginSelectionWindow();
        selectionWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Database
        services.AddDbContext<BillingDbContext>();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Services
        services.AddSingleton<ISessionContext, SessionContext>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IAuthService, AuthService>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginSelectionViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CustomerListViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<SupplierListViewModel>();
        services.AddTransient<BillingViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<BackupViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    private void ApplySavedTheme()
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var settings = context.Settings.FirstOrDefault(s => s.Id == 1);
            if (settings != null)
            {
                ChangeTheme(settings.Theme);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply saved theme. Defaulting to Light.");
        }
    }

    public static void ChangeTheme(string themeName)
    {
        var app = (App)System.Windows.Application.Current;
        var merged = app.Resources.MergedDictionaries;
        
        // Find existing theme dictionary
        var existingTheme = merged.FirstOrDefault(d => 
            d.Source != null && 
            (d.Source.OriginalString.Contains("LightTheme.xaml") || 
             d.Source.OriginalString.Contains("DarkTheme.xaml")));

        if (existingTheme != null)
        {
            merged.Remove(existingTheme);
        }

        // Load new theme
        var newTheme = new ResourceDictionary
        {
            Source = new Uri($"Themes/{themeName}Theme.xaml", UriKind.Relative)
        };
        
        // Insert at 0 so controls styles (Styles.xaml) can reference dynamic resource brushes
        merged.Insert(0, newTheme);
        Log.Information($"UI Theme changed to {themeName}");
    }
}
