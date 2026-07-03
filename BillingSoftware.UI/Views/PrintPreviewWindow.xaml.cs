using System;
using System.Diagnostics;
using System.Windows;

namespace BillingSoftware.UI.Views;

public partial class PrintPreviewWindow : Window
{
    private readonly string _pdfPath;

    public PrintPreviewWindow(string pdfPath)
    {
        InitializeComponent();
        _pdfPath = pdfPath;
        Loaded += PrintPreviewWindow_Loaded;
    }

    private void PrintPreviewWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // WebBrowser Navigate requires absolute Uri
            pdfBrowser.Navigate(new Uri(_pdfPath));
        }
        catch (Exception ex)
        {
            MessageBox.Show("Unable to load preview internally. You can click 'Open in System PDF' to view it.\nError: " + ex.Message, "Preview Load Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var printProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pdfPath,
                    Verb = "print",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                }
            };
            printProcess.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Direct print command failed. Please use 'Open in System PDF' and select Print there.\nDetails: " + ex.Message, "Print Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenExternal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(_pdfPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Unable to open PDF externally: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
