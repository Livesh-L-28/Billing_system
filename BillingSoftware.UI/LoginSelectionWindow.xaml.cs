using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using BillingSoftware.UI.ViewModels;

namespace BillingSoftware.UI;

public partial class LoginSelectionWindow : Window
{
    public LoginSelectionWindow()
    {
        InitializeComponent();

        // Resolve ViewModel from Dependency Injection
        var viewModel = App.ServiceProvider.GetRequiredService<LoginSelectionViewModel>();
        DataContext = viewModel;

        // Subscribe to Success Event
        viewModel.LoginSuccess += OnLoginSuccess;

        // Initialize passwords on load and focus the admin username text box
        Loaded += (s, e) =>
        {
            txtAdminUsername.Focus();
        };
    }

    private void OnLoginSuccess()
    {
        // Open MainWindow
        var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        // Close LoginSelectionWindow
        Close();
    }

    private void SwitchToManualLogin_Click(object sender, RoutedEventArgs e)
    {
        // Open standard LoginWindow
        var loginWindow = new LoginWindow();
        loginWindow.Show();

        // Close LoginSelectionWindow
        Close();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.CloseWindow(this);
    }
}
