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

public class ProductListViewModel : ViewModelBase
{
    private readonly BillingDbContext _dbContext;
    private readonly IAuditLogger _auditLogger;

    private ObservableCollection<Product> _products = new();
    public ObservableCollection<Product> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    private ObservableCollection<Category> _categories = new();
    public ObservableCollection<Category> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    private ObservableCollection<Unit> _units = new();
    public ObservableCollection<Unit> Units
    {
        get => _units;
        set => SetProperty(ref _units, value);
    }

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                _ = LoadProductsAsync();
            }
        }
    }

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                EditSelectedCommand.NotifyCanExecuteChanged();
                DeleteProductCommand.NotifyCanExecuteChanged();
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

    private string _formDescription = string.Empty;
    public string FormDescription
    {
        get => _formDescription;
        set => SetProperty(ref _formDescription, value);
    }

    private Category? _formCategory;
    public Category? FormCategory
    {
        get => _formCategory;
        set => SetProperty(ref _formCategory, value);
    }

    private Unit? _formUnit;
    public Unit? FormUnit
    {
        get => _formUnit;
        set => SetProperty(ref _formUnit, value);
    }

    private string _formHSNCode = string.Empty;
    public string FormHSNCode
    {
        get => _formHSNCode;
        set => SetProperty(ref _formHSNCode, value);
    }

    private decimal _formGSTPercentage = 5;
    public decimal FormGSTPercentage
    {
        get => _formGSTPercentage;
        set => SetProperty(ref _formGSTPercentage, value);
    }

    private decimal _formPurchasePrice;
    public decimal FormPurchasePrice
    {
        get => _formPurchasePrice;
        set => SetProperty(ref _formPurchasePrice, value);
    }

    private decimal _formSalesPrice;
    public decimal FormSalesPrice
    {
        get => _formSalesPrice;
        set => SetProperty(ref _formSalesPrice, value);
    }

    private decimal _formOpeningStock;
    public decimal FormOpeningStock
    {
        get => _formOpeningStock;
        set => SetProperty(ref _formOpeningStock, value);
    }

    private decimal _formMinimumStockLevel;
    public decimal FormMinimumStockLevel
    {
        get => _formMinimumStockLevel;
        set => SetProperty(ref _formMinimumStockLevel, value);
    }

    private bool _formIsActive = true;
    public bool FormIsActive
    {
        get => _formIsActive;
        set => SetProperty(ref _formIsActive, value);
    }

    public ObservableCollection<decimal> GSTRates { get; } = new() { 0, 5, 12, 18, 28 };

    // Commands
    public ICommand AddNewCommand { get; }
    public RelayCommand EditSelectedCommand { get; }
    public ICommand CancelEditCommand { get; }
    public IAsyncRelayCommand SaveProductCommand { get; }
    public AsyncRelayCommand DeleteProductCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ImportExcelCommand { get; }

    public ProductListViewModel(BillingDbContext dbContext, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _auditLogger = auditLogger;

        AddNewCommand = new RelayCommand(ExecuteAddNew);
        EditSelectedCommand = new RelayCommand(ExecuteEditSelected, () => SelectedProduct != null);
        CancelEditCommand = new RelayCommand(ExecuteCancelEdit);
        SaveProductCommand = new AsyncRelayCommand(SaveProductAsync);
        DeleteProductCommand = new AsyncRelayCommand(DeleteProductAsync, () => SelectedProduct != null);
        ExportExcelCommand = new RelayCommand(ExportToExcel);
        ImportExcelCommand = new RelayCommand(ImportFromExcel);

        Title = "Products Registry";
        _ = InitializeDataAsync();
    }

    private async Task InitializeDataAsync()
    {
        await LoadLookupListsAsync();
        await LoadProductsAsync();
    }

    private async Task LoadLookupListsAsync()
    {
        try
        {
            var cats = await _dbContext.Categories.OrderBy(c => c.Name).ToListAsync();
            Categories = new ObservableCollection<Category>(cats);

            var unts = await _dbContext.Units.OrderBy(u => u.Code).ToListAsync();
            Units = new ObservableCollection<Unit>(unts);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error loading registries: " + ex.Message);
        }
    }

    public async Task LoadProductsAsync()
    {
        IsBusy = true;
        try
        {
            var query = _dbContext.Products.Include(p => p.Category).Include(p => p.Unit).AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var normQuery = SearchQuery.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(normQuery) || 
                                         p.HSNCode.Contains(normQuery) || 
                                         (p.Category != null && p.Category.Name.ToLower().Contains(normQuery)));
            }

            var list = await query.OrderBy(p => p.Name).ToListAsync();
            Products = new ObservableCollection<Product>(list);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error loading products: " + ex.Message);
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
        FormDescription = string.Empty;
        FormCategory = Categories.FirstOrDefault(c => c.Name == "TOWEL") ?? Categories.FirstOrDefault();
        FormUnit = Units.FirstOrDefault(u => u.Code == "PCS") ?? Units.FirstOrDefault();
        FormHSNCode = string.Empty;
        FormGSTPercentage = 5;
        FormPurchasePrice = 0;
        FormSalesPrice = 0;
        FormOpeningStock = 0;
        FormMinimumStockLevel = 0;
        FormIsActive = true;

        IsEditing = true;
    }

    private void ExecuteEditSelected()
    {
        if (SelectedProduct == null) return;

        _formId = SelectedProduct.Id;
        FormName = SelectedProduct.Name;
        FormDescription = SelectedProduct.Description;
        FormCategory = Categories.FirstOrDefault(c => c.Id == SelectedProduct.CategoryId) ?? Categories.FirstOrDefault(c => c.Name == "TOWEL") ?? Categories.FirstOrDefault();
        FormUnit = Units.FirstOrDefault(u => u.Id == SelectedProduct.UnitId) ?? Units.FirstOrDefault(u => u.Code == "PCS") ?? Units.FirstOrDefault();
        FormHSNCode = SelectedProduct.HSNCode;
        FormGSTPercentage = SelectedProduct.GSTPercentage;
        FormPurchasePrice = SelectedProduct.PurchasePrice;
        FormSalesPrice = SelectedProduct.SalesPrice;
        FormOpeningStock = SelectedProduct.OpeningStock;
        FormMinimumStockLevel = SelectedProduct.MinimumStockLevel;
        FormIsActive = SelectedProduct.IsActive;

        IsEditing = true;
    }

    private void ExecuteCancelEdit()
    {
        IsEditing = false;
    }

    private async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            System.Windows.MessageBox.Show("Product Name is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(FormHSNCode))
        {
            System.Windows.MessageBox.Show("HSN Code is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (FormCategory == null)
        {
            System.Windows.MessageBox.Show("Please select a Category.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (FormUnit == null)
        {
            System.Windows.MessageBox.Show("Please select a Unit of measure.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            Product? product;
            bool isNew = _formId == Guid.Empty;

            if (isNew)
            {
                product = new Product { Id = Guid.NewGuid() };
            }
            else
            {
                product = await _dbContext.Products.FindAsync(_formId);
                if (product == null)
                {
                    System.Windows.MessageBox.Show("Product not found in database.");
                    return;
                }
            }

            product.Name = FormName;
            product.Description = FormDescription;
            product.CategoryId = FormCategory.Id;
            product.UnitId = FormUnit.Id;
            product.HSNCode = FormHSNCode;
            product.GSTPercentage = FormGSTPercentage;
            product.PurchasePrice = FormPurchasePrice;
            product.SalesPrice = FormSalesPrice;
            product.OpeningStock = FormOpeningStock;
            product.MinimumStockLevel = FormMinimumStockLevel;
            product.IsActive = FormIsActive;

            // If new, current stock matches opening stock
            if (isNew)
            {
                product.CurrentStock = FormOpeningStock;
            }
            else
            {
                // Simple recalculation: if opening stock changed, adjust current stock
                decimal stockDiff = FormOpeningStock - product.OpeningStock;
                product.CurrentStock += stockDiff;
            }

            if (isNew)
            {
                await _dbContext.Products.AddAsync(product);
                // Log stock entry
                await _dbContext.Stocks.AddAsync(new Stock
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    TransactionType = "Opening",
                    Quantity = product.OpeningStock,
                    Remarks = "Opening stock registration"
                });
            }
            else
            {
                _dbContext.Products.Update(product);
            }

            await _dbContext.SaveChangesAsync();
            await _auditLogger.LogActivityAsync(isNew ? "Create Product" : "Update Product", "Product", $"Saved product {FormName}");

            IsEditing = false;
            await LoadProductsAsync();

            System.Windows.MessageBox.Show("Product saved successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error saving product: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteProductAsync()
    {
        if (SelectedProduct == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete product '{SelectedProduct.Name}'?", 
            "Confirm Delete", 
            System.Windows.MessageBoxButton.YesNo, 
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        // Check if referenced in invoices or purchases
        bool referenced = await _dbContext.InvoiceItems.AnyAsync(i => i.ProductId == SelectedProduct.Id) || 
                           await _dbContext.PurchaseItems.AnyAsync(i => i.ProductId == SelectedProduct.Id);
        if (referenced)
        {
            System.Windows.MessageBox.Show(
                "Cannot delete this product because it is referenced in sales or purchases history. You can mark it as Inactive instead.", 
                "Reference Error", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            string name = SelectedProduct.Name;
            _dbContext.Products.Remove(SelectedProduct);
            await _dbContext.SaveChangesAsync();
            await _auditLogger.LogActivityAsync("Delete Product", "Product", $"Deleted product {name}");

            SelectedProduct = null;
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error deleting product: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            FileName = "Products_List_" + DateTime.Now.ToString("yyyyMMdd")
        };
        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                // Export simple mapping DTO of products to avoid navigation circular reference loops
                var exportList = Products.Select(p => new
                {
                    p.Name,
                    Category = p.Category?.Name ?? "General",
                    Unit = p.Unit?.Code ?? "PCS",
                    p.HSNCode,
                    p.GSTPercentage,
                    p.PurchasePrice,
                    p.SalesPrice,
                    p.OpeningStock,
                    p.CurrentStock,
                    p.MinimumStockLevel,
                    Status = p.IsActive ? "Active" : "Inactive"
                }).ToList();

                ExcelHelper.ExportToExcel(exportList, saveDialog.FileName, "Products");
                System.Windows.MessageBox.Show("Products catalog exported successfully.", "Export Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Export failed: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void ImportFromExcel()
    {
        if (!Categories.Any() || !Units.Any())
        {
            System.Windows.MessageBox.Show("Please setup at least one Category and Unit in database before importing.", "Import Pre-requisite", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var openDialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            Title = "Select Product Import File"
        };
        if (openDialog.ShowDialog() == true)
        {
            try
            {
                var defaultCatId = Categories.First().Id;
                var defaultUnitId = Units.First().Id;

                var list = ExcelHelper.ImportProducts(openDialog.FileName, defaultCatId, defaultUnitId);
                if (list.Count == 0)
                {
                    System.Windows.MessageBox.Show("No valid product records found in Excel sheet.", "Import Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var confirm = System.Windows.MessageBox.Show(
                    $"Parsed {list.Count} products. Import them? (Categories will default to '{Categories.First().Name}')", 
                    "Confirm Bulk Import", 
                    System.Windows.MessageBoxButton.YesNo, 
                    System.Windows.MessageBoxImage.Question);

                if (confirm == System.Windows.MessageBoxResult.Yes)
                {
                    _dbContext.Products.AddRange(list);
                    
                    // Add opening stock ledger entries
                    foreach (var product in list)
                    {
                        _dbContext.Stocks.Add(new Stock
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            TransactionType = "Opening",
                            Quantity = product.OpeningStock,
                            Remarks = "Excel bulk import"
                        });
                    }

                    _dbContext.SaveChanges();
                    _auditLogger.LogActivityAsync("Bulk Import Products", "Product", $"Imported {list.Count} products").Wait();
                    _ = LoadProductsAsync();

                    System.Windows.MessageBox.Show($"{list.Count} products imported successfully.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Import failed: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
