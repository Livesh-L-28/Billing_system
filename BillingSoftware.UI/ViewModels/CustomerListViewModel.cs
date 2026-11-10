using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using BillingSoftware.Core.Entities;
using BillingSoftware.Application.Services;
using BillingSoftware.Database;
using BillingSoftware.Reporting;

namespace BillingSoftware.UI.ViewModels;

public class CustomerListViewModel : ViewModelBase
{
    private readonly BillingDbContext _dbContext;
    private readonly IAuditLogger _auditLogger;

    private ObservableCollection<Customer> _customers = new();
    public ObservableCollection<Customer> Customers
    {
        get => _customers;
        set => SetProperty(ref _customers, value);
    }

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = LoadCustomersAsync();
            }
        }
    }

    private Customer? _selectedCustomer;
    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                EditSelectedCommand.NotifyCanExecuteChanged();
                DeleteCustomerCommand.NotifyCanExecuteChanged();
            }
        }
    }

    // Form Binding Properties
    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    private Guid _formId;
    private string _formName = string.Empty;
    public string FormName
    {
        get => _formName;
        set => SetProperty(ref _formName, value);
    }

    private string _formGSTIN = string.Empty;
    public string FormGSTIN
    {
        get => _formGSTIN;
        set => SetProperty(ref _formGSTIN, value);
    }

    private string _formPAN = string.Empty;
    public string FormPAN
    {
        get => _formPAN;
        set => SetProperty(ref _formPAN, value);
    }

    private string _formMobile = string.Empty;
    public string FormMobile
    {
        get => _formMobile;
        set => SetProperty(ref _formMobile, value);
    }

    private string _formEmail = string.Empty;
    public string FormEmail
    {
        get => _formEmail;
        set => SetProperty(ref _formEmail, value);
    }

    private string _formAddress = string.Empty;
    public string FormAddress
    {
        get => _formAddress;
        set => SetProperty(ref _formAddress, value);
    }

    private string _formCity = string.Empty;
    public string FormCity
    {
        get => _formCity;
        set => SetProperty(ref _formCity, value);
    }

    private string _formState = "Maharashtra";
    public string FormState
    {
        get => _formState;
        set => SetProperty(ref _formState, value);
    }

    private string _formPincode = string.Empty;
    public string FormPincode
    {
        get => _formPincode;
        set => SetProperty(ref _formPincode, value);
    }

    private decimal _formOpeningBalance;
    public decimal FormOpeningBalance
    {
        get => _formOpeningBalance;
        set => SetProperty(ref _formOpeningBalance, value);
    }

    private int _formCreditDays = 30;
    public int FormCreditDays
    {
        get => _formCreditDays;
        set => SetProperty(ref _formCreditDays, value);
    }

    private string _formCustomerType = "Unregistered"; // Registered, Unregistered, Composition
    public string FormCustomerType
    {
        get => _formCustomerType;
        set => SetProperty(ref _formCustomerType, value);
    }

    private bool _formIsActive = true;
    public bool FormIsActive
    {
        get => _formIsActive;
        set => SetProperty(ref _formIsActive, value);
    }

    public ObservableCollection<string> States { get; } = new()
    {
        "Andhra Pradesh", "Arunachal Pradesh", "Assam", "Bihar", "Chhattisgarh", "Goa", 
        "Gujarat", "Haryana", "Himachal Pradesh", "Jharkhand", "Karnataka", "Kerala", 
        "Madhya Pradesh", "Maharashtra", "Manipur", "Meghalaya", "Mizoram", "Nagaland", 
        "Odisha", "Punjab", "Rajasthan", "Sikkim", "Tamil Nadu", "Telangana", "Tripura", 
        "Uttar Pradesh", "Uttarakhand", "West Bengal", "Delhi", "Jammu and Kashmir"
    };

    public ObservableCollection<string> CustomerTypes { get; } = new() { "Registered", "Unregistered", "Composition" };

    // Commands
    public ICommand AddNewCommand { get; }
    public RelayCommand EditSelectedCommand { get; }
    public ICommand CancelEditCommand { get; }
    public IAsyncRelayCommand SaveCustomerCommand { get; }
    public AsyncRelayCommand DeleteCustomerCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ImportExcelCommand { get; }

    public CustomerListViewModel(BillingDbContext dbContext, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _auditLogger = auditLogger;

        AddNewCommand = new RelayCommand(ExecuteAddNew);
        EditSelectedCommand = new RelayCommand(ExecuteEditSelected, () => SelectedCustomer != null);
        CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
        SaveCustomerCommand = new AsyncRelayCommand(SaveCustomerAsync);
        DeleteCustomerCommand = new AsyncRelayCommand(DeleteCustomerAsync, () => SelectedCustomer != null);
        ExportExcelCommand = new RelayCommand(ExportToExcel);
        ImportExcelCommand = new RelayCommand(ImportFromExcel);

        Title = "Customers Master";
        _ = LoadCustomersAsync();
    }

    public async Task LoadCustomersAsync()
    {
        IsBusy = true;
        try
        {
            var query = _dbContext.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var normQuery = SearchQuery.ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(normQuery) || 
                                         c.Mobile.Contains(normQuery) || 
                                         c.GSTIN.Contains(normQuery));
            }

            var list = await query.OrderBy(c => c.Name).ToListAsync();
            Customers = new ObservableCollection<Customer>(list);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error loading customers: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExecuteAddNew()
    {
        _formId = Guid.Empty;
        FormName = string.Empty;
        FormGSTIN = string.Empty;
        FormPAN = string.Empty;
        FormMobile = string.Empty;
        FormEmail = string.Empty;
        FormAddress = string.Empty;
        FormCity = string.Empty;
        FormState = "Maharashtra";
        FormPincode = string.Empty;
        FormOpeningBalance = 0;
        FormCreditDays = 30;
        FormCustomerType = "Unregistered";
        FormIsActive = true;

        IsEditing = true;
    }

    private void ExecuteEditSelected()
    {
        if (SelectedCustomer == null) return;

        _formId = SelectedCustomer.Id;
        FormName = SelectedCustomer.Name;
        FormGSTIN = SelectedCustomer.GSTIN;
        FormPAN = SelectedCustomer.PAN;
        FormMobile = SelectedCustomer.Mobile;
        FormEmail = SelectedCustomer.Email;
        FormAddress = SelectedCustomer.Address;
        FormCity = SelectedCustomer.City;
        FormState = States.Contains(SelectedCustomer.State) ? SelectedCustomer.State : "Maharashtra";
        FormPincode = SelectedCustomer.Pincode;
        FormOpeningBalance = SelectedCustomer.OpeningBalance;
        FormCreditDays = SelectedCustomer.CreditDays;
        FormCustomerType = CustomerTypes.Contains(SelectedCustomer.CustomerType) ? SelectedCustomer.CustomerType : "Unregistered";
        FormIsActive = SelectedCustomer.IsActive;

        IsEditing = true;
    }

    private void ExecuteCancelEdit()
    {
        IsEditing = false;
    }

    private async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            System.Windows.MessageBox.Show("Customer Name is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(FormMobile))
        {
            System.Windows.MessageBox.Show("Mobile Number is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        string cleanGstin = FormGSTIN?.Trim() ?? string.Empty;
        if (FormCustomerType == "Registered" && (string.IsNullOrWhiteSpace(cleanGstin) || cleanGstin.Length != 15))
        {
            System.Windows.MessageBox.Show("A valid 15-digit GSTIN is required for Registered customers.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            Customer? customer;
            bool isNew = _formId == Guid.Empty;

            if (isNew)
            {
                customer = new Customer { Id = Guid.NewGuid() };
            }
            else
            {
                customer = await _dbContext.Customers.FindAsync(_formId);
                if (customer == null)
                {
                    System.Windows.MessageBox.Show("Customer not found in database.");
                    return;
                }
            }

            customer.Name = FormName?.Trim() ?? string.Empty;
            customer.GSTIN = cleanGstin;
            customer.PAN = FormPAN?.Trim() ?? string.Empty;
            customer.Mobile = FormMobile?.Trim() ?? string.Empty;
            customer.Email = FormEmail?.Trim() ?? string.Empty;
            customer.Address = FormAddress?.Trim() ?? string.Empty;
            customer.City = FormCity?.Trim() ?? string.Empty;
            customer.State = FormState;
            customer.Pincode = FormPincode?.Trim() ?? string.Empty;
            customer.OpeningBalance = FormOpeningBalance;
            customer.OutstandingBalance = FormOpeningBalance; // Initial calculation
            customer.CreditDays = FormCreditDays;
            customer.CustomerType = FormCustomerType;
            customer.IsActive = FormIsActive;

            if (isNew)
            {
                await _dbContext.Customers.AddAsync(customer);
            }
            else
            {
                _dbContext.Customers.Update(customer);
            }

            await _dbContext.SaveChangesAsync();
            await _auditLogger.LogActivityAsync(isNew ? "Create Customer" : "Update Customer", "Customer", $"Saved customer {FormName}");

            IsEditing = false;
            await LoadCustomersAsync();
            
            System.Windows.MessageBox.Show("Customer saved successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error saving customer: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete customer '{SelectedCustomer.Name}'?", 
            "Confirm Delete", 
            System.Windows.MessageBoxButton.YesNo, 
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        // Check if customer has associated invoices
        bool hasInvoices = await _dbContext.Invoices.AnyAsync(i => i.CustomerId == SelectedCustomer.Id);
        if (hasInvoices)
        {
            System.Windows.MessageBox.Show(
                "Cannot delete this customer because they have sales invoices recorded. You can mark them as Inactive instead.", 
                "Reference Error", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            string name = SelectedCustomer.Name;
            _dbContext.Customers.Remove(SelectedCustomer);
            await _dbContext.SaveChangesAsync();
            await _auditLogger.LogActivityAsync("Delete Customer", "Customer", $"Deleted customer {name}");

            SelectedCustomer = null;
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error deleting customer: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExportToExcel()
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = "Customers_List_" + DateTime.Now.ToString("yyyyMMdd")
        };
        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                ExcelHelper.ExportToExcel(Customers, saveDialog.FileName, "Customers");
                System.Windows.MessageBox.Show("Customers exported successfully to Excel.", "Export Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Excel export failed: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void ImportFromExcel()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            Title = "Select Customer Import File"
        };
        if (openDialog.ShowDialog() == true)
        {
            try
            {
                var list = ExcelHelper.ImportCustomers(openDialog.FileName);
                if (list.Count == 0)
                {
                    System.Windows.MessageBox.Show("No valid customer rows found in the select Excel file.", "Import Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var confirm = System.Windows.MessageBox.Show(
                    $"Found {list.Count} customers in Excel file. Do you want to bulk-insert them into the database?", 
                    "Confirm Import", 
                    System.Windows.MessageBoxButton.YesNo, 
                    System.Windows.MessageBoxImage.Question);

                if (confirm == System.Windows.MessageBoxResult.Yes)
                {
                    _dbContext.Customers.AddRange(list);
                    _dbContext.SaveChanges();
                    _auditLogger.LogActivityAsync("Bulk Import Customers", "Customer", $"Imported {list.Count} customers via Excel").Wait();
                    _ = LoadCustomersAsync();
                    System.Windows.MessageBox.Show($"{list.Count} customers imported successfully.", "Import Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Excel import failed: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
