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

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly BillingDbContext _dbContext;

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
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

    public IAsyncRelayCommand<object> LoginCommand { get; }
    public ICommand LoadDataCommand { get; }

    public event Action? LoginSuccess;

    public LoginViewModel(IAuthService authService, BillingDbContext dbContext)
    {
        _authService = authService;
        _dbContext = dbContext;

        LoginCommand = new AsyncRelayCommand<object>(ExecuteLoginAsync, CanLogin);
        LoadDataCommand = new RelayCommand(LoadFinancialYears);

        Title = "User Login";
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

    private async Task ExecuteLoginAsync(object? parameter)
    {
        // Parameter is expected to be a PasswordBox
        if (parameter is not System.Windows.Controls.PasswordBox passwordBox)
        {
            ErrorMessage = "Invalid password capture control.";
            return;
        }

        string password = passwordBox.Password;
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Please enter username.";
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Please enter password.";
            return;
        }

        if (SelectedFinancialYear == null)
        {
            ErrorMessage = "Please select financial year.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        LoginCommand.NotifyCanExecuteChanged();

        try
        {
            // Run auth in background task to keep UI responsive
            bool success = await Task.Run(() => 
                _authService.AuthenticateAsync(Username, password, SelectedFinancialYear.Id));

            if (success)
            {
                LoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = "Invalid username or password.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error during login: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
            LoginCommand.NotifyCanExecuteChanged();
        }
    }
}
