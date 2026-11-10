using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using BillingSoftware.Core.Entities;
using BillingSoftware.Application.Services;
using BillingSoftware.Database;

namespace BillingSoftware.UI.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly BillingDbContext _dbContext;
    private readonly ISessionContext _sessionContext;
    private readonly IAuditLogger _auditLogger;

    // Company Properties
    private string _companyName = string.Empty;
    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }

    private string _gstin = string.Empty;
    public string GSTIN
    {
        get => _gstin;
        set => SetProperty(ref _gstin, value);
    }

    private string _pan = string.Empty;
    public string PAN
    {
        get => _pan;
        set => SetProperty(ref _pan, value);
    }

    private string _address = string.Empty;
    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    private string _phone = string.Empty;
    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string _bankName = string.Empty;
    public string BankName
    {
        get => _bankName;
        set => SetProperty(ref _bankName, value);
    }

    private string _bankAccountNumber = string.Empty;
    public string BankAccountNumber
    {
        get => _bankAccountNumber;
        set => SetProperty(ref _bankAccountNumber, value);
    }

    private string _bankIFSC = string.Empty;
    public string BankIFSC
    {
        get => _bankIFSC;
        set => SetProperty(ref _bankIFSC, value);
    }

    private string _bankBranch = string.Empty;
    public string BankBranch
    {
        get => _bankBranch;
        set => SetProperty(ref _bankBranch, value);
    }

    private string _termsAndConditions = string.Empty;
    public string TermsAndConditions
    {
        get => _termsAndConditions;
        set => SetProperty(ref _termsAndConditions, value);
    }

    private string _selectedState = "Maharashtra";
    public string SelectedState
    {
        get => _selectedState;
        set => SetProperty(ref _selectedState, value);
    }

    private string? _logoPath;
    public string? LogoPath
    {
        get => _logoPath;
        set => SetProperty(ref _logoPath, value);
    }

    private string? _signaturePath;
    public string? SignaturePath
    {
        get => _signaturePath;
        set => SetProperty(ref _signaturePath, value);
    }

    // App Settings Properties
    private string _invoicePrefix = "INV";
    public string InvoicePrefix
    {
        get => _invoicePrefix;
        set => SetProperty(ref _invoicePrefix, value);
    }

    private string _invoiceFormat = "{Prefix}-{Year}-{Number:D4}";
    public string InvoiceFormat
    {
        get => _invoiceFormat;
        set => SetProperty(ref _invoiceFormat, value);
    }

    private bool _enableRoundOff = true;
    public bool EnableRoundOff
    {
        get => _enableRoundOff;
        set => SetProperty(ref _enableRoundOff, value);
    }

    private string _selectedTheme = "Light";
    public string SelectedTheme
    {
        get => _selectedTheme;
        set => SetProperty(ref _selectedTheme, value);
    }

    private string _backupFolderPath = string.Empty;
    public string BackupFolderPath
    {
        get => _backupFolderPath;
        set => SetProperty(ref _backupFolderPath, value);
    }

    private string _defaultPrinterName = string.Empty;
    public string DefaultPrinterName
    {
        get => _defaultPrinterName;
        set => SetProperty(ref _defaultPrinterName, value);
    }

    private string _invoiceStorageFolderPath = string.Empty;
    public string InvoiceStorageFolderPath
    {
        get => _invoiceStorageFolderPath;
        set => SetProperty(ref _invoiceStorageFolderPath, value);
    }

    // Lists
    public ObservableCollection<string> Themes { get; } = new() { "Light", "Dark" };
    public ObservableCollection<string> States { get; } = new()
    {
        "Andhra Pradesh", "Arunachal Pradesh", "Assam", "Bihar", "Chhattisgarh", "Goa", 
        "Gujarat", "Haryana", "Himachal Pradesh", "Jharkhand", "Karnataka", "Kerala", 
        "Madhya Pradesh", "Maharashtra", "Manipur", "Meghalaya", "Mizoram", "Nagaland", 
        "Odisha", "Punjab", "Rajasthan", "Sikkim", "Tamil Nadu", "Telangana", "Tripura", 
        "Uttar Pradesh", "Uttarakhand", "West Bengal", "Delhi", "Jammu and Kashmir"
    };

    public ObservableCollection<string> Printers { get; } = new();

    // Commands
    public ICommand SaveCompanyCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand SelectLogoCommand { get; }
    public ICommand SelectSignatureCommand { get; }
    public ICommand SelectBackupPathCommand { get; }
    public ICommand SelectInvoiceStoragePathCommand { get; }

    public SettingsViewModel(BillingDbContext dbContext, ISessionContext sessionContext, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _sessionContext = sessionContext;
        _auditLogger = auditLogger;

        SaveCompanyCommand = new AsyncRelayCommand(SaveCompanyAsync);
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        SelectLogoCommand = new RelayCommand(SelectLogo);
        SelectSignatureCommand = new RelayCommand(SelectSignature);
        SelectBackupPathCommand = new RelayCommand(SelectBackupPath);
        SelectInvoiceStoragePathCommand = new RelayCommand(SelectInvoiceStoragePath);

        Title = "Settings";
        
        LoadData();
        LoadSystemPrinters();
    }

    private void LoadData()
    {
        try
        {
            // Load Company
            var company = _dbContext.Companies.FirstOrDefault(c => c.Id == _sessionContext.CurrentCompanyId);
            if (company != null)
            {
                CompanyName = company.CompanyName;
                GSTIN = company.GSTIN;
                PAN = company.PAN;
                Address = company.Address;
                Phone = company.Phone;
                Email = company.Email;
                BankName = company.BankName;
                BankAccountNumber = company.BankAccountNumber;
                BankIFSC = company.BankIFSC;
                BankBranch = company.BankBranch;
                TermsAndConditions = company.TermsAndConditions;
                SelectedState = States.Contains(company.State) ? company.State : "Maharashtra";
                LogoPath = company.LogoPath;
                SignaturePath = company.SignaturePath;
            }

            // Load Settings
            var setting = _dbContext.Settings.FirstOrDefault(s => s.Id == _sessionContext.CurrentSettingId);
            if (setting != null)
            {
                InvoicePrefix = setting.InvoicePrefix;
                InvoiceFormat = setting.InvoiceFormat;
                EnableRoundOff = setting.EnableRoundOff;
                SelectedTheme = Themes.Contains(setting.Theme) ? setting.Theme : "Light";
                BackupFolderPath = setting.BackupFolderPath;
                DefaultPrinterName = setting.DefaultPrinterName;
                InvoiceStorageFolderPath = setting.InvoiceStorageFolderPath;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to load settings: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void LoadSystemPrinters()
    {
        try
        {
            Printers.Clear();
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                Printers.Add(printer);
            }
        }
        catch
        {
            // Fallback if no printer system is attached
            Printers.Add("Default Printer");
        }
    }

    private async Task SaveCompanyAsync()
    {
        IsBusy = true;
        try
        {
            var company = _dbContext.Companies.FirstOrDefault(c => c.Id == _sessionContext.CurrentCompanyId) ?? new Company { Id = _sessionContext.CurrentCompanyId };
            
            company.CompanyName = CompanyName?.Trim() ?? string.Empty;
            company.GSTIN = GSTIN?.Trim() ?? string.Empty;
            company.PAN = PAN?.Trim() ?? string.Empty;
            company.Address = Address?.Trim() ?? string.Empty;
            company.Phone = Phone?.Trim() ?? string.Empty;
            company.Email = Email?.Trim() ?? string.Empty;
            company.BankName = BankName?.Trim() ?? string.Empty;
            company.BankAccountNumber = BankAccountNumber?.Trim() ?? string.Empty;
            company.BankIFSC = BankIFSC?.Trim() ?? string.Empty;
            company.BankBranch = BankBranch?.Trim() ?? string.Empty;
            company.TermsAndConditions = TermsAndConditions?.Trim() ?? string.Empty;
            company.State = SelectedState;
            company.LogoPath = LogoPath;
            company.SignaturePath = SignaturePath;

            _dbContext.Companies.Update(company);
            await _dbContext.SaveChangesAsync();

            await _auditLogger.LogActivityAsync("Update Company Profile", "Settings", "Updated Company Details");
            System.Windows.MessageBox.Show("Company profile saved successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to save company profile: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSettingsAsync()
    {
        IsBusy = true;
        try
        {
            var setting = _dbContext.Settings.FirstOrDefault(s => s.Id == _sessionContext.CurrentSettingId) ?? new Setting { Id = _sessionContext.CurrentSettingId };

            string oldTheme = setting.Theme;

            setting.InvoicePrefix = InvoicePrefix;
            setting.InvoiceFormat = InvoiceFormat;
            setting.EnableRoundOff = EnableRoundOff;
            setting.Theme = SelectedTheme;
            setting.BackupFolderPath = BackupFolderPath;
            setting.DefaultPrinterName = DefaultPrinterName;
            setting.InvoiceStorageFolderPath = InvoiceStorageFolderPath?.Trim() ?? string.Empty;

            _dbContext.Settings.Update(setting);
            await _dbContext.SaveChangesAsync();

            // Toggle dynamic theme immediately if it changed
            if (oldTheme != SelectedTheme)
            {
                App.ChangeTheme(SelectedTheme);
            }

            await _auditLogger.LogActivityAsync("Update Settings", "Settings", "Updated general application settings");
            System.Windows.MessageBox.Show("Application settings saved successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to save settings: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SelectLogo()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            Title = "Select Company Logo"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            // Copy file to local media directory
            var mediaDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BillingSoftware", "Media");
            if (!Directory.Exists(mediaDir)) Directory.CreateDirectory(mediaDir);

            var destFile = Path.Combine(mediaDir, "logo" + Path.GetExtension(openFileDialog.FileName));
            File.Copy(openFileDialog.FileName, destFile, true);
            LogoPath = destFile;
        }
    }

    private void SelectSignature()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            Title = "Select Digital Signature"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            var mediaDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BillingSoftware", "Media");
            if (!Directory.Exists(mediaDir)) Directory.CreateDirectory(mediaDir);

            var destFile = Path.Combine(mediaDir, "signature" + Path.GetExtension(openFileDialog.FileName));
            File.Copy(openFileDialog.FileName, destFile, true);
            SignaturePath = destFile;
        }
    }

    private void SelectBackupPath()
    {
        // Custom folder browser dialog (using Microsoft.Win32 in modern versions or standard System.Windows.Forms)
        // Since WPF .NET 8 standard OpenFolderDialog is now available in Microsoft.Win32!
        var dialog = new OpenFolderDialog
        {
            Title = "Select Backup Folder"
        };
        if (dialog.ShowDialog() == true)
        {
            BackupFolderPath = dialog.FolderName;
        }
    }

    private void SelectInvoiceStoragePath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Invoice Storage Folder"
        };
        if (dialog.ShowDialog() == true)
        {
            InvoiceStorageFolderPath = dialog.FolderName;
        }
    }
}
