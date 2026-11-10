using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Core.Entities;
using BillingSoftware.Application.Services;
using BillingSoftware.Database;

namespace BillingSoftware.UI.ViewModels;

public class LoginSelectionViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly BillingDbContext _dbContext;

    private string _adminUsername = "admin";
    public string AdminUsername
    {
        get => _adminUsername;
        set => SetProperty(ref _adminUsername, value);
    }

    private string _cashierUsername = "cashier";
    public string CashierUsername
    {
        get => _cashierUsername;
        set => SetProperty(ref _cashierUsername, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private ObservableCollection<FinancialYear> _financialYears = new();
    public ObservableCollection<FinancialYear> FinancialYears
    {
        get => _financialYears;
        set => SetProperty(ref _financialYears, value);
    }

    private FinancialYear? _selectedFinancialYear;
    public FinancialYear? SelectedFinancialYear
    {
        get => _selectedFinancialYear;
        set => SetProperty(ref _selectedFinancialYear, value);
    }

    public IAsyncRelayCommand<object> LoginAdminCommand { get; }
    public IAsyncRelayCommand<object> LoginCashierCommand { get; }
    public ICommand LoadDataCommand { get; }

    public event Action? LoginSuccess;

    public LoginSelectionViewModel(IAuthService authService, BillingDbContext dbContext)
    {
        _authService = authService;
        _dbContext = dbContext;

        LoginAdminCommand = new AsyncRelayCommand<object>(ExecuteAdminLoginAsync, CanLogin);
        LoginCashierCommand = new AsyncRelayCommand<object>(ExecuteCashierLoginAsync, CanLogin);
        LoadDataCommand = new RelayCommand(LoadFinancialYears);

        Title = "Terminal Selection";
        LoadFinancialYears();
    }

    private void LoadFinancialYears()
    {
        try
        {
            var years = _dbContext.FinancialYears.OrderByDescending(y => y.StartDate).ToList();
            FinancialYears = new ObservableCollection<FinancialYear>(years);
            SelectedFinancialYear = years.FirstOrDefault(y => y.IsActive) ?? years.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Database connection failed: " + ex.Message;
        }
    }

    private bool CanLogin(object? parameter)
    {
        return !IsBusy && SelectedFinancialYear != null;
    }

    private async Task ExecuteAdminLoginAsync(object? parameter)
    {
        if (parameter is not System.Windows.Controls.PasswordBox passwordBox)
        {
            ErrorMessage = "Invalid password control for Admin.";
            return;
        }

        string password = passwordBox.Password;
        if (string.IsNullOrWhiteSpace(AdminUsername))
        {
            ErrorMessage = "Please enter Admin username.";
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Please enter Admin password.";
            return;
        }

        await ExecuteLoginAsync(AdminUsername, password);
    }

    private async Task ExecuteCashierLoginAsync(object? parameter)
    {
        if (parameter is not System.Windows.Controls.PasswordBox passwordBox)
        {
            ErrorMessage = "Invalid password control for Cashier.";
            return;
        }

        string password = passwordBox.Password;
        if (string.IsNullOrWhiteSpace(CashierUsername))
        {
            ErrorMessage = "Please enter Cashier username.";
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Please enter Cashier password.";
            return;
        }

        await ExecuteLoginAsync(CashierUsername, password);
    }

    private async Task ExecuteLoginAsync(string username, string password)
    {
        if (SelectedFinancialYear == null)
        {
            ErrorMessage = "Please select financial year.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        NotifyCommandsCanExecuteChanged();

        try
        {
            bool success = await Task.Run(() => 
                _authService.AuthenticateAsync(username, password, SelectedFinancialYear.Id));

            if (success)
            {
                LoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = $"Invalid credentials for '{username}'.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error during login: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyCommandsCanExecuteChanged();
        }
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        LoginAdminCommand.NotifyCanExecuteChanged();
        LoginCashierCommand.NotifyCanExecuteChanged();
    }
}
