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

public class SupplierListViewModel : ViewModelBase
{
    private readonly BillingDbContext _dbContext;
    private readonly IAuditLogger _auditLogger;

    private ObservableCollection<Supplier> _suppliers = new();
    public ObservableCollection<Supplier> Suppliers
    {
        get => _suppliers;
        set => SetProperty(ref _suppliers, value);
    }

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = LoadSuppliersAsync();
            }
        }
    }

    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                EditSelectedCommand.NotifyCanExecuteChanged();
                DeleteSupplierCommand.NotifyCanExecuteChanged();
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

    private string _formAddress = string.Empty;
    public string FormAddress
    {
        get => _formAddress;
        set => SetProperty(ref _formAddress, value);
    }

    private string _formPhone = string.Empty;
    public string FormPhone
    {
        get => _formPhone;
        set => SetProperty(ref _formPhone, value);
    }

    private string _formEmail = string.Empty;
    public string FormEmail
    {
        get => _formEmail;
        set => SetProperty(ref _formEmail, value);
    }

    private string _formBankName = string.Empty;
    public string FormBankName
    {
        get => _formBankName;
        set => SetProperty(ref _formBankName, value);
    }

    private string _formBankAccountNo = string.Empty;
    public string FormBankAccountNo
    {
        get => _formBankAccountNo;
        set => SetProperty(ref _formBankAccountNo, value);
    }

    private string _formBankIFSC = string.Empty;
    public string FormBankIFSC
    {
        get => _formBankIFSC;
        set => SetProperty(ref _formBankIFSC, value);
    }

    private decimal _formOutstandingBalance;
    public decimal FormOutstandingBalance
    {
        get => _formOutstandingBalance;
        set => SetProperty(ref _formOutstandingBalance, value);
    }

    private bool _formIsActive = true;
    public bool FormIsActive
    {
        get => _formIsActive;
        set => SetProperty(ref _formIsActive, value);
    }

    // Commands
    public ICommand AddNewCommand { get; }
    public RelayCommand EditSelectedCommand { get; }
    public ICommand CancelEditCommand { get; }
    public IAsyncRelayCommand SaveSupplierCommand { get; }
    public AsyncRelayCommand DeleteSupplierCommand { get; }

    public SupplierListViewModel(BillingDbContext dbContext, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _auditLogger = auditLogger;

        AddNewCommand = new RelayCommand(ExecuteAddNew);
        EditSelectedCommand = new RelayCommand(ExecuteEditSelected, () => SelectedSupplier != null);
        CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
        SaveSupplierCommand = new AsyncRelayCommand(SaveSupplierAsync);
        DeleteSupplierCommand = new AsyncRelayCommand(DeleteSupplierAsync, () => SelectedSupplier != null);

        Title = "Suppliers Master";
        _ = LoadSuppliersAsync();
    }

    public async Task LoadSuppliersAsync()
    {
        IsBusy = true;
        try
        {
            var query = _dbContext.Suppliers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var normQuery = SearchQuery.ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(normQuery) || 
                                         s.Phone.Contains(normQuery) || 
                                         s.GSTIN.Contains(normQuery));
            }

            var list = await query.OrderBy(s => s.Name).ToListAsync();
            Suppliers = new ObservableCollection<Supplier>(list);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error loading suppliers: " + ex.Message);
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
        FormAddress = string.Empty;
        FormPhone = string.Empty;
        FormEmail = string.Empty;
        FormBankName = string.Empty;
        FormBankAccountNo = string.Empty;
        FormBankIFSC = string.Empty;
        FormOutstandingBalance = 0;
        FormIsActive = true;

        IsEditing = true;
    }

    private void ExecuteEditSelected()
    {
        if (SelectedSupplier == null) return;

        _formId = SelectedSupplier.Id;
        FormName = SelectedSupplier.Name;
        FormGSTIN = SelectedSupplier.GSTIN;
        FormAddress = SelectedSupplier.Address;
        FormPhone = SelectedSupplier.Phone;
        FormEmail = SelectedSupplier.Email;
        FormBankName = SelectedSupplier.BankName;
        FormBankAccountNo = SelectedSupplier.BankAccountNo;
        FormBankIFSC = SelectedSupplier.BankIFSC;
        FormOutstandingBalance = SelectedSupplier.OutstandingBalance;
        FormIsActive = SelectedSupplier.IsActive;

        IsEditing = true;
    }

    private void ExecuteCancelEdit()
    {
        IsEditing = false;
    }

    private async Task SaveSupplierAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            System.Windows.MessageBox.Show("Supplier Name is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            Supplier? supplier;
            bool isNew = _formId == Guid.Empty;

            if (isNew)
            {
                supplier = new Supplier { Id = Guid.NewGuid() };
            }
            else
            {
                supplier = await _dbContext.Suppliers.FindAsync(_formId);
                if (supplier == null)
                {
                    System.Windows.MessageBox.Show("Supplier not found in database.");
                    return;
                }
            }

            supplier.Name = FormName;
            supplier.GSTIN = FormGSTIN;
            supplier.Address = FormAddress;
            supplier.Phone = FormPhone;
            supplier.Email = FormEmail;
            supplier.BankName = FormBankName;
            supplier.BankAccountNo = FormBankAccountNo;
            supplier.BankIFSC = FormBankIFSC;
            supplier.OutstandingBalance = FormOutstandingBalance;
            supplier.IsActive = FormIsActive;

            if (isNew)
            {
                await _dbContext.Suppliers.AddAsync(supplier);
            }
            else
            {
                _dbContext.Suppliers.Update(supplier);
            }

            await _dbContext.SaveChangesAsync();
            await _auditLogger.LogActivityAsync(isNew ? "Create Supplier" : "Update Supplier", "Supplier", $"Saved supplier {FormName}");

            IsEditing = false;
            await LoadSuppliersAsync();

            System.Windows.MessageBox.Show("Supplier saved successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error saving supplier: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteSupplierAsync()
    {
        if (SelectedSupplier == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete supplier '{SelectedSupplier.Name}'?", 
            "Confirm Delete", 
            System.Windows.MessageBoxButton.YesNo, 
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        // Check reference in purchases
        bool referenced = await _dbContext.Purchases.AnyAsync(p => p.SupplierId == SelectedSupplier.Id);
        if (referenced)
        {
            System.Windows.MessageBox.Show(
                "Cannot delete this supplier because there are purchase entries recorded against them. You can mark them as Inactive instead.", 
                "Reference Error", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            string name = SelectedSupplier.Name;
            _dbContext.Suppliers.Remove(SelectedSupplier);
            await _dbContext.SaveChangesAsync();
            await _auditLogger.LogActivityAsync("Delete Supplier", "Supplier", $"Deleted supplier {name}");

            SelectedSupplier = null;
            await LoadSuppliersAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error deleting supplier: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
