using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using BillingSoftware.Core.Entities;
using BillingSoftware.Application.Services;
using BillingSoftware.Database;
using BillingSoftware.Backup;
using Microsoft.EntityFrameworkCore;

namespace BillingSoftware.UI.ViewModels;

public class BackupViewModel : ViewModelBase
{
    private readonly BillingDbContext _dbContext;
    private readonly IAuditLogger _auditLogger;
    private readonly BackupService _backupService;

    private string _backupFolderPath = string.Empty;
    public string BackupFolderPath
    {
        get => _backupFolderPath;
        set => SetProperty(ref _backupFolderPath, value);
    }

    private string _statusMessage = "Ready for operations.";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // Commands
    public ICommand SelectBackupPathCommand { get; }
    public ICommand CreateBackupCommand { get; }
    public ICommand RestoreBackupCommand { get; }

    public BackupViewModel(BillingDbContext dbContext, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _auditLogger = auditLogger;
        _backupService = new BackupService(); // Native class instantiation

        SelectBackupPathCommand = new RelayCommand(SelectBackupPath);
        CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync);
        RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);

        Title = "Backup & Recovery Console";
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            var setting = _dbContext.Settings.FirstOrDefault(s => s.Id == 1);
            if (setting != null && !string.IsNullOrWhiteSpace(setting.BackupFolderPath))
            {
                BackupFolderPath = setting.BackupFolderPath;
            }
            else
            {
                // Default fallback
                var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BillingSoftware", "Backups");
                BackupFolderPath = defaultPath;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to load default backup folder: " + ex.Message;
        }
    }

    private void SelectBackupPath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose Backup Folder"
        };
        if (dialog.ShowDialog() == true)
        {
            BackupFolderPath = dialog.FolderName;

            // Save to settings
            try
            {
                var setting = _dbContext.Settings.FirstOrDefault(s => s.Id == 1) ?? new Setting { Id = 1 };
                setting.BackupFolderPath = BackupFolderPath;
                _dbContext.Settings.Update(setting);
                _dbContext.SaveChanges();
            }
            catch
            {
                // Ignore silent DB fails
            }
        }
    }

    private async Task CreateBackupAsync()
    {
        if (string.IsNullOrWhiteSpace(BackupFolderPath))
        {
            System.Windows.MessageBox.Show("Please select a target backup folder first.", "Path Required", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        StatusMessage = "Creating backup package...";

        try
        {
            // Database path from DbContext connection string
            var connectionString = _dbContext.Database.GetDbConnection().ConnectionString;
            
            // connectionString typically is "Data Source=C:\Users\...\billing.db"
            // We parse the file path out of it
            string dbFilePath = connectionString.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();

            // Destination zip file name e.g. Billing_Backup_20260702_142341.zip
            string fileName = $"Billing_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            string destZipPath = Path.Combine(BackupFolderPath, fileName);

            await Task.Run(() => _backupService.CreateBackup(dbFilePath, destZipPath));

            await _auditLogger.LogActivityAsync("Create Backup", "Maintenance", $"Backed up database file to {fileName}");
            
            StatusMessage = $"Backup saved successfully to {fileName}";
            System.Windows.MessageBox.Show($"Backup saved successfully:\n{fileName}", "Backup Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = "Backup creation failed: " + ex.Message;
            System.Windows.MessageBox.Show("Backup failed:\n" + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RestoreBackupAsync()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "Backup Files (*.zip)|*.zip",
            Title = "Select Backup Package to Restore"
        };

        if (openDialog.ShowDialog() != true) return;

        var confirm = System.Windows.MessageBox.Show(
            "RESTORE WARNING:\n\nRestoring a backup will overwrite your current active database! All sales records, customers, and inventory data added since the backup was taken will be lost.\n\nAre you sure you want to proceed?",
            "Confirm Database Restore",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsBusy = true;
        StatusMessage = "Executing database restoration...";

        try
        {
            var connectionString = _dbContext.Database.GetDbConnection().ConnectionString;
            string dbFilePath = connectionString.Replace("Data Source=", "", StringComparison.OrdinalIgnoreCase).Trim();

            // Perform restoration
            await Task.Run(() => _backupService.RestoreBackup(openDialog.FileName, dbFilePath));

            await _auditLogger.LogActivityAsync("Restore Backup", "Maintenance", "Restored database from zip archive");

            StatusMessage = "Restoration complete! Restarting application is required.";
            System.Windows.MessageBox.Show("Restoration completed successfully.\n\nThe application will now close. Please restart it to load the restored database.", "Restore Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            
            // Shut down application to reload SQLite database file
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            StatusMessage = "Restoration failed: " + ex.Message;
            System.Windows.MessageBox.Show("Restoration failed:\n" + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
