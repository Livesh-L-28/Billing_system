using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using BillingSoftware.UI.ViewModels;

namespace BillingSoftware.UI;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        // Resolve ViewModel from Dependency Injection
        var viewModel = App.ServiceProvider.GetRequiredService<LoginViewModel>();
        DataContext = viewModel;

        // Focus the username text box
        Loaded += (s, e) => txtUsername.Focus();

        // Subscribe to Success Event
        viewModel.LoginSuccess += OnLoginSuccess;
    }

    private void OnLoginSuccess()
    {
        // Open MainWindow
        var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        // Close LoginWindow
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
