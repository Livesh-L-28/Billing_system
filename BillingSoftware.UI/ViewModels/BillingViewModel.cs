using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using BillingSoftware.Core.Entities;
using BillingSoftware.Core.Repositories;
using BillingSoftware.Application.Services;
using BillingSoftware.Database;
using BillingSoftware.Common;
using System.IO;

namespace BillingSoftware.UI.ViewModels;

public class BillingViewModel : ViewModelBase
{
    private readonly BillingDbContext _dbContext;
    private readonly ISessionContext _sessionContext;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAuditLogger _auditLogger;

    // In-memory Lists
    public ObservableCollection<Customer> Customers { get; private set; } = new();
    public ObservableCollection<Product> Products { get; private set; } = new();
    public ObservableCollection<InvoiceItem> InvoiceItems { get; } = new();

    public System.ComponentModel.ICollectionView CustomersView { get; }
    public System.ComponentModel.ICollectionView ProductsView { get; }

    private string _customerSearchText = string.Empty;
    public string CustomerSearchText
    {
        get => _customerSearchText;
        set
        {
            if (SetProperty(ref _customerSearchText, value))
            {
                if (SelectedCustomer != null && string.Equals(SelectedCustomer.Name, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                CustomersView.Refresh();
            }
        }
    }

    private string _productSearchText = string.Empty;
    public string ProductSearchText
    {
        get => _productSearchText;
        set
        {
            if (SetProperty(ref _productSearchText, value))
            {
                if (SelectedProduct != null && string.Equals(SelectedProduct.Name, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                ProductsView.Refresh();
            }
        }
    }

    // Selectors
    private Customer? _selectedCustomer;
    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value))
            {
                OnCustomerChanged();
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
                OnProductChanged();
                AddItemCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    // Invoice Header Fields
    private string _formattedInvoiceNumber = string.Empty;
    public string FormattedInvoiceNumber
    {
        get => _formattedInvoiceNumber;
        set => SetProperty(ref _formattedInvoiceNumber, value);
    }

    private bool _isManualInvoiceNumber;
    public bool IsManualInvoiceNumber
    {
        get => _isManualInvoiceNumber;
        set
        {
            if (SetProperty(ref _isManualInvoiceNumber, value))
            {
                _ = OnManualInvoiceNumberChangedAsync();
            }
        }
    }

    private int _nextSequenceNumber;

    private DateTime _invoiceDate = DateTime.Today;
    public DateTime InvoiceDate
    {
        get => _invoiceDate;
        set
        {
            if (SetProperty(ref _invoiceDate, value))
            {
                UpdateDueDate();
            }
        }
    }

    private DateTime? _dueDate;
    public DateTime? DueDate
    {
        get => _dueDate;
        set => SetProperty(ref _dueDate, value);
    }

    private string _billingAddress = string.Empty;
    public string BillingAddress
    {
        get => _billingAddress;
        set => SetProperty(ref _billingAddress, value);
    }

    private string _shippingAddress = string.Empty;
    public string ShippingAddress
    {
        get => _shippingAddress;
        set => SetProperty(ref _shippingAddress, value);
    }

    private string _poInvoiceNumber = string.Empty;
    public string POInvoiceNumber
    {
        get => _poInvoiceNumber;
        set => SetProperty(ref _poInvoiceNumber, value);
    }

    private DateTime? _poDate;
    public DateTime? PODate
    {
        get => _poDate;
        set => SetProperty(ref _poDate, value);
    }

    private string _paymentMethod = "Credit"; // Cash, Credit, Bank
    public string PaymentMethod
    {
        get => _paymentMethod;
        set => SetProperty(ref _paymentMethod, value);
    }

    // Line Item Fields
    private decimal _lineQuantity = 1;
    public decimal LineQuantity
    {
        get => _lineQuantity;
        set
        {
            if (SetProperty(ref _lineQuantity, value))
            {
                UpdateLineTotal();
                AddItemCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    private decimal _lineRate;
    public decimal LineRate
    {
        get => _lineRate;
        set
        {
            if (SetProperty(ref _lineRate, value))
            {
                UpdateLineTotal();
                AddItemCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    private decimal _lineDiscountPercent;
    public decimal LineDiscountPercent
    {
        get => _lineDiscountPercent;
        set
        {
            if (SetProperty(ref _lineDiscountPercent, value))
            {
                UpdateLineTotal();
            }
        }
    }

    private decimal _lineTotal;
    public decimal LineTotal
    {
        get => _lineTotal;
        set => SetProperty(ref _lineTotal, value);
    }

    // Financial Year totals
    private decimal _subTotal;
    public decimal SubTotal
    {
        get => _subTotal;
        set => SetProperty(ref _subTotal, value);
    }

    private decimal _discountTotal;
    public decimal DiscountTotal
    {
        get => _discountTotal;
        set => SetProperty(ref _discountTotal, value);
    }

    private decimal _cgstTotal;
    public decimal CGSTTotal
    {
        get => _cgstTotal;
        set => SetProperty(ref _cgstTotal, value);
    }

    private decimal _sgstTotal;
    public decimal SGSTTotal
    {
        get => _sgstTotal;
        set => SetProperty(ref _sgstTotal, value);
    }

    private decimal _igstTotal;
    public decimal IGSTTotal
    {
        get => _igstTotal;
        set => SetProperty(ref _igstTotal, value);
    }

    private decimal _roundOff;
    public decimal RoundOff
    {
        get => _roundOff;
        set => SetProperty(ref _roundOff, value);
    }

    private decimal _grandTotal;
    public decimal GrandTotal
    {
        get => _grandTotal;
        set => SetProperty(ref _grandTotal, value);
    }

    private string _amountInWords = string.Empty;
    public string AmountInWords
    {
        get => _amountInWords;
        set => SetProperty(ref _amountInWords, value);
    }

    // Lists for binding
    public ObservableCollection<string> PaymentMethods { get; } = new() { "Cash", "Bank", "Credit" };

    private Guid? _editingInvoiceId;
    public Guid? EditingInvoiceId
    {
        get => _editingInvoiceId;
        set
        {
            if (SetProperty(ref _editingInvoiceId, value))
            {
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(IsNewMode));
            }
        }
    }

    public bool IsEditMode => EditingInvoiceId.HasValue;
    public bool IsNewMode => !IsEditMode;

    private readonly Task _initializationTask;

    // Commands
    public RelayCommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public IAsyncRelayCommand SaveInvoiceCommand { get; }
    public IAsyncRelayCommand PrintInvoiceCommand { get; }
    public ICommand ClearFormCommand { get; }

    public BillingViewModel(
        BillingDbContext dbContext,
        ISessionContext sessionContext,
        IInvoiceRepository invoiceRepository,
        IProductRepository productRepository,
        IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _sessionContext = sessionContext;
        _invoiceRepository = invoiceRepository;
        _productRepository = productRepository;
        _auditLogger = auditLogger;

        AddItemCommand = new RelayCommand(ExecuteAddItem, CanAddItem);
        RemoveItemCommand = new RelayCommand<InvoiceItem>(ExecuteRemoveItem);
        SaveInvoiceCommand = new AsyncRelayCommand(SaveInvoiceAsync, CanSaveInvoice);
        PrintInvoiceCommand = new AsyncRelayCommand(PrintInvoiceAsync, CanSaveInvoice);
        ClearFormCommand = new RelayCommand(ClearForm);

        CustomersView = System.Windows.Data.CollectionViewSource.GetDefaultView(Customers);
        CustomersView.Filter = FilterCustomerItem;

        ProductsView = System.Windows.Data.CollectionViewSource.GetDefaultView(Products);
        ProductsView.Filter = FilterProductItem;

        Title = "New Tax Invoice";
        _initializationTask = LoadLookupsAsync();
    }

    private bool FilterCustomerItem(object obj)
    {
        if (obj is Customer customer)
        {
            if (string.IsNullOrWhiteSpace(CustomerSearchText))
                return true;

            if (SelectedCustomer != null && SelectedCustomer.Id == customer.Id)
                return true;

            return customer.Name.Contains(CustomerSearchText, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private bool FilterProductItem(object obj)
    {
        if (obj is Product product)
        {
            if (string.IsNullOrWhiteSpace(ProductSearchText))
                return true;

            if (SelectedProduct != null && SelectedProduct.Id == product.Id)
                return true;

            return product.Name.Contains(ProductSearchText, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            var custs = await _dbContext.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            Customers.Clear();
            foreach (var c in custs)
            {
                Customers.Add(c);
            }
            OnPropertyChanged(nameof(Customers));

            var prods = await _dbContext.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            Products.Clear();
            foreach (var p in prods)
            {
                Products.Add(p);
            }
            OnPropertyChanged(nameof(Products));

            await LoadNextInvoiceNumberAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to load selectors: " + ex.Message);
        }
    }

    public async Task LoadInvoiceForEditingAsync(string invoiceNo)
    {
        if (_initializationTask != null)
        {
            await _initializationTask;
        }

        if (string.IsNullOrWhiteSpace(invoiceNo)) return;

        try
        {
            var invoice = await _dbContext.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceItems)
                .ThenInclude(ii => ii.Product)
                .FirstOrDefaultAsync(i => i.FormattedInvoiceNumber == invoiceNo);

            if (invoice == null)
            {
                System.Windows.MessageBox.Show("Invoice not found in database.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            Title = $"Edit Invoice: {invoice.FormattedInvoiceNumber}";
            EditingInvoiceId = invoice.Id;
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == invoice.CustomerId);
            
            if (SelectedCustomer != null)
            {
                CustomerSearchText = SelectedCustomer.Name;
            }

            InvoiceDate = invoice.InvoiceDate;
            DueDate = invoice.DueDate;
            BillingAddress = invoice.BillingAddress;
            ShippingAddress = invoice.ShippingAddress;
            POInvoiceNumber = invoice.POInvoiceNumber;
            PODate = invoice.PODate;
            PaymentMethod = invoice.PaymentMethod;

            InvoiceItems.Clear();
            foreach (var item in invoice.InvoiceItems)
            {
                InvoiceItems.Add(item);
            }

            FormattedInvoiceNumber = invoice.FormattedInvoiceNumber;
            IsManualInvoiceNumber = true; // allow text display of number

            RecalculateInvoiceTotals();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error loading invoice for editing: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task LoadNextInvoiceNumberAsync()
    {
        var activeFY = _sessionContext.CurrentFinancialYear;
        if (activeFY == null) return;

        try
        {
            int nextNumber = await _invoiceRepository.GetNextInvoiceNumberAsync(activeFY.Id);
            _nextSequenceNumber = nextNumber;
            
            if (!IsManualInvoiceNumber)
            {
                var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Id == _sessionContext.CurrentSettingId) ?? new Setting { Id = _sessionContext.CurrentSettingId };
                string fySuffix = activeFY.Name; // "2026-27"
                FormattedInvoiceNumber = setting.InvoiceFormat
                    .Replace("{Prefix}", setting.InvoicePrefix)
                    .Replace("{Year}", fySuffix)
                    .Replace("{Number:D4}", nextNumber.ToString("D4"))
                    .Replace("{Number:D5}", nextNumber.ToString("D5"));
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to load invoice number: " + ex.Message);
        }
    }

    private async Task OnManualInvoiceNumberChangedAsync()
    {
        if (!IsManualInvoiceNumber)
        {
            await LoadNextInvoiceNumberAsync();
        }
    }

    private int ParseInvoiceNumber(string formattedNo)
    {
        var match = System.Text.RegularExpressions.Regex.Match(formattedNo, @"\d+$");
        if (match.Success && int.TryParse(match.Value, out int num))
        {
            return num;
        }
        match = System.Text.RegularExpressions.Regex.Match(formattedNo, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int num2))
        {
            return num2;
        }
        return 0;
    }

    private void OnCustomerChanged()
    {
        if (SelectedCustomer == null)
        {
            BillingAddress = string.Empty;
            ShippingAddress = string.Empty;
            DueDate = null;
            return;
        }

        BillingAddress = SelectedCustomer.Address;
        ShippingAddress = SelectedCustomer.Address;
        UpdateDueDate();
        
        // Recalculate taxes for all items since customer (and thus supply state) changed
        foreach (var item in InvoiceItems)
        {
            CalculateTaxesForItem(item);
        }

        RecalculateInvoiceTotals();
    }

    private void OnProductChanged()
    {
        if (SelectedProduct == null)
        {
            LineRate = 0;
            LineQuantity = 1;
            LineDiscountPercent = 0;
            return;
        }

        LineRate = SelectedProduct.SalesPrice;
        LineQuantity = 1;
        LineDiscountPercent = 0;
        UpdateLineTotal();
    }

    private void UpdateDueDate()
    {
        if (SelectedCustomer == null) return;
        DueDate = InvoiceDate.AddDays(SelectedCustomer.CreditDays);
    }

    private void UpdateLineTotal()
    {
        decimal rawTotal = LineQuantity * LineRate;
        decimal discAmt = rawTotal * (LineDiscountPercent / 100);
        LineTotal = rawTotal - discAmt;
    }

    private bool CanAddItem()
    {
        return SelectedProduct != null && LineQuantity > 0 && LineRate >= 0;
    }

    private void ExecuteAddItem()
    {
        if (SelectedProduct == null) return;

        // Check if item already added, if so merge quantity
        var existing = InvoiceItems.FirstOrDefault(item => item.ProductId == SelectedProduct.Id);
        if (existing != null)
        {
            existing.Quantity += LineQuantity;
            existing.Rate = LineRate;
            existing.DiscountPercent = LineDiscountPercent;
            
            // Recompute values
            decimal rawTotal = existing.Quantity * existing.Rate;
            existing.DiscountAmount = rawTotal * (existing.DiscountPercent / 100);
            existing.TaxableValue = rawTotal - existing.DiscountAmount;
            
            // Recalculate tax
            CalculateTaxesForItem(existing);
        }
        else
        {
            var item = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                ProductId = SelectedProduct.Id,
                ProductName = SelectedProduct.Name?.Trim()?.ToUpperInvariant() ?? "",
                HSNCode = SelectedProduct.HSNCode?.Trim()?.ToUpperInvariant() ?? "",
                Quantity = LineQuantity,
                Rate = LineRate,
                DiscountPercent = LineDiscountPercent,
                GSTPercentage = SelectedProduct.GSTPercentage
            };

            decimal rawTotal = item.Quantity * item.Rate;
            item.DiscountAmount = rawTotal * (item.DiscountPercent / 100);
            item.TaxableValue = rawTotal - item.DiscountAmount;

            CalculateTaxesForItem(item);
            InvoiceItems.Add(item);
        }

        // Reset inputs
        SelectedProduct = null;
        RecalculateInvoiceTotals();
    }

    private void CalculateTaxesForItem(InvoiceItem item)
    {
        // Always apply CGST + SGST (Half-half) and set IGST to 0
        decimal halfRate = item.GSTPercentage / 2;
        item.CGSTAmount = Math.Round(item.TaxableValue * (halfRate / 100), 2);
        item.SGSTAmount = Math.Round(item.TaxableValue * (halfRate / 100), 2);
        item.IGSTAmount = 0;

        item.TotalAmount = item.TaxableValue + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
    }

    public static bool IsIntraState(string compState, string compGstin, string custState, string custGstin)
    {
        compGstin = (compGstin ?? "").Trim();
        custGstin = (custGstin ?? "").Trim();

        // 1. If both have GSTIN, compare state codes (first 2 digits)
        if (compGstin.Length >= 2 && custGstin.Length >= 2 &&
            char.IsDigit(compGstin[0]) && char.IsDigit(compGstin[1]) &&
            char.IsDigit(custGstin[0]) && char.IsDigit(custGstin[1]))
        {
            string compStateCode = compGstin.Substring(0, 2);
            string custStateCode = custGstin.Substring(0, 2);
            if (compStateCode == custStateCode)
            {
                return true;
            }
        }

        // 2. Fallback to robust normalized state names
        return NormalizeStateName(compState) == NormalizeStateName(custState);
    }

    public static string NormalizeStateName(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName)) return "";
        string s = stateName.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "");
        if (s == "tn") return "tamilnadu";
        if (s == "mh") return "maharashtra";
        if (s == "dl") return "delhi";
        return s;
    }

    private void ExecuteRemoveItem(InvoiceItem? item)
    {
        if (item == null) return;
        InvoiceItems.Remove(item);
        RecalculateInvoiceTotals();
    }

    private void RecalculateInvoiceTotals()
    {
        decimal sub = 0;
        decimal disc = 0;
        decimal cgst = 0;
        decimal sgst = 0;
        decimal igst = 0;

        foreach (var item in InvoiceItems)
        {
            sub += item.TaxableValue;
            disc += item.DiscountAmount;
            cgst += item.CGSTAmount;
            sgst += item.SGSTAmount;
            igst += item.IGSTAmount;
        }

        SubTotal = sub;
        DiscountTotal = disc;
        CGSTTotal = cgst;
        SGSTTotal = sgst;
        IGSTTotal = igst;

        decimal exactTotal = sub + cgst + sgst + igst;
        
        // Settings for Rounding
        var settings = _dbContext.Settings.FirstOrDefault(s => s.Id == 1);
        bool enableRound = settings?.EnableRoundOff ?? true;

        if (enableRound)
        {
            decimal roundedTotal = Math.Round(exactTotal, 0);
            RoundOff = roundedTotal - exactTotal;
            GrandTotal = roundedTotal;
        }
        else
        {
            RoundOff = 0;
            GrandTotal = Math.Round(exactTotal, 2);
        }

        AmountInWords = NumberToWords.ConvertAmount(GrandTotal);
        SaveInvoiceCommand.NotifyCanExecuteChanged();
        PrintInvoiceCommand.NotifyCanExecuteChanged();
    }

    private bool CanSaveInvoice()
    {
        return SelectedCustomer != null && InvoiceItems.Count > 0 && !IsBusy;
    }

    private async Task SaveInvoiceAsync()
    {
        await SaveInvoiceInternalAsync(printInvoice: false);
    }

    private async Task PrintInvoiceAsync()
    {
        await SaveInvoiceInternalAsync(printInvoice: true);
    }

    private async Task SaveInvoiceInternalAsync(bool printInvoice)
    {
        if (SelectedCustomer == null) return;
        if (InvoiceItems.Count == 0) return;

        IsBusy = true;
        SaveInvoiceCommand.NotifyCanExecuteChanged();
        PrintInvoiceCommand.NotifyCanExecuteChanged();

        try
        {
            var activeFY = _sessionContext.CurrentFinancialYear;
            if (activeFY == null)
            {
                System.Windows.MessageBox.Show("No active financial year found in session.");
                return;
            }

            if (activeFY.IsLocked)
            {
                System.Windows.MessageBox.Show("The current financial year is locked. Operations are read-only.", "Permission Lock", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Retrieve Prefix formatting details
            var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Id == _sessionContext.CurrentSettingId) ?? new Setting { Id = _sessionContext.CurrentSettingId };
            
            int invoiceNumToSave = 0;
            string formattedNumToSave = string.Empty;

            if (IsEditMode)
            {
                invoiceNumToSave = ParseInvoiceNumber(FormattedInvoiceNumber);
                formattedNumToSave = FormattedInvoiceNumber.Trim();
            }
            else if (IsManualInvoiceNumber)
            {
                if (string.IsNullOrWhiteSpace(FormattedInvoiceNumber))
                {
                    System.Windows.MessageBox.Show("Invoice number cannot be empty when manual overrides are enabled.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                int parsedInvoiceNumber = ParseInvoiceNumber(FormattedInvoiceNumber);
                if (parsedInvoiceNumber <= 0)
                {
                    System.Windows.MessageBox.Show("Please include a numeric sequence number in the custom invoice number (e.g. INV-0001).", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Check for duplicates
                bool numExists = await _dbContext.Invoices.AnyAsync(i => i.InvoiceNumber == parsedInvoiceNumber && i.FinancialYearId == activeFY.Id);
                if (numExists)
                {
                    System.Windows.MessageBox.Show($"The sequence number {parsedInvoiceNumber} is already used in this financial year.", "Duplicate Number", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                bool formattedExists = await _dbContext.Invoices.AnyAsync(i => i.FormattedInvoiceNumber == FormattedInvoiceNumber);
                if (formattedExists)
                {
                    System.Windows.MessageBox.Show($"The invoice number '{FormattedInvoiceNumber}' already exists.", "Duplicate Invoice Number", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                invoiceNumToSave = parsedInvoiceNumber;
                formattedNumToSave = FormattedInvoiceNumber.Trim();
            }
            else
            {
                // Re-evaluate next sequence number at save time to prevent concurrency issues
                int nextNumber = await _invoiceRepository.GetNextInvoiceNumberAsync(activeFY.Id);
                string fySuffix = activeFY.Name;
                string formattedNum = setting.InvoiceFormat
                    .Replace("{Prefix}", setting.InvoicePrefix)
                    .Replace("{Year}", fySuffix)
                    .Replace("{Number:D4}", nextNumber.ToString("D4"))
                    .Replace("{Number:D5}", nextNumber.ToString("D5"));

                // Just in case of concurrency duplicate
                bool formattedExists = await _dbContext.Invoices.AnyAsync(i => i.FormattedInvoiceNumber == formattedNum);
                if (formattedExists)
                {
                    System.Windows.MessageBox.Show($"The generated invoice number '{formattedNum}' already exists. Please try saving again to use the updated sequence.", "Duplicate Invoice Number", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    // Refresh next invoice number in UI
                    _ = LoadNextInvoiceNumberAsync();
                    return;
                }

                invoiceNumToSave = nextNumber;
                formattedNumToSave = formattedNum;
            }

            Invoice? invoice = null;

            if (IsEditMode)
            {
                invoice = await _dbContext.Invoices
                    .Include(i => i.InvoiceItems)
                    .Include(i => i.Payments)
                    .FirstOrDefaultAsync(i => i.Id == EditingInvoiceId!.Value);

                if (invoice == null)
                {
                    System.Windows.MessageBox.Show("Existing invoice to edit was not found.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                // 1. Revert previous stock changes
                foreach (var oldItem in invoice.InvoiceItems)
                {
                    var oldProduct = await _dbContext.Products.FindAsync(oldItem.ProductId);
                    if (oldProduct != null)
                    {
                        oldProduct.CurrentStock += oldItem.Quantity;
                        _dbContext.Products.Update(oldProduct);

                        // Add a stock correction record
                        await _dbContext.Stocks.AddAsync(new Stock
                        {
                            Id = Guid.NewGuid(),
                            ProductId = oldProduct.Id,
                            TransactionType = "SaleCorrection",
                            Quantity = oldItem.Quantity,
                            ReferenceId = invoice.Id,
                            Remarks = $"Stock Reversion for Edit Invoice {invoice.FormattedInvoiceNumber}"
                        });
                    }
                }

                // 2. Revert previous customer outstanding balance if old payment was Credit
                if (invoice.PaymentMethod == "Credit")
                {
                    var oldCustomer = await _dbContext.Customers.FindAsync(invoice.CustomerId);
                    if (oldCustomer != null)
                    {
                        oldCustomer.OutstandingBalance -= invoice.GrandTotal;
                        _dbContext.Customers.Update(oldCustomer);
                    }
                }

                // 3. Remove old payments
                var oldPayments = invoice.Payments.ToList();
                if (oldPayments.Any())
                {
                    _dbContext.Payments.RemoveRange(oldPayments);
                    invoice.Payments.Clear();
                }

                // 4. Remove old items
                var oldItems = invoice.InvoiceItems.ToList();
                if (oldItems.Any())
                {
                    _dbContext.InvoiceItems.RemoveRange(oldItems);
                    invoice.InvoiceItems.Clear();
                }
            }
            else
            {
                invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _sessionContext.CurrentUser?.Id ?? Guid.Empty
                };
            }

            // Update main properties
            invoice.InvoiceNumber = invoiceNumToSave;
            invoice.FormattedInvoiceNumber = formattedNumToSave?.Trim()?.ToUpperInvariant() ?? "";
            invoice.InvoiceDate = InvoiceDate;
            invoice.FinancialYearId = activeFY.Id;
            invoice.CustomerId = SelectedCustomer.Id;
            invoice.BillingAddress = BillingAddress?.Trim()?.ToUpperInvariant() ?? "";
            invoice.ShippingAddress = ShippingAddress?.Trim()?.ToUpperInvariant() ?? "";
            invoice.POInvoiceNumber = POInvoiceNumber?.Trim()?.ToUpperInvariant() ?? "";
            invoice.PODate = PODate;
            invoice.DueDate = DueDate;
            invoice.PaymentMethod = PaymentMethod;
            invoice.SubTotal = SubTotal;
            invoice.DiscountAmount = DiscountTotal;
            invoice.CGSTTotal = CGSTTotal;
            invoice.SGSTTotal = SGSTTotal;
            invoice.IGSTTotal = IGSTTotal;
            invoice.RoundOff = RoundOff;
            invoice.GrandTotal = GrandTotal;
            invoice.IsCancelled = false;

            // Copy items to invoice entity
            foreach (var uiItem in InvoiceItems)
            {
                var newItem = new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ProductId = uiItem.ProductId,
                    ProductName = uiItem.ProductName?.Trim()?.ToUpperInvariant() ?? "",
                    HSNCode = uiItem.HSNCode?.Trim()?.ToUpperInvariant() ?? "",
                    Quantity = uiItem.Quantity,
                    Rate = uiItem.Rate,
                    DiscountPercent = uiItem.DiscountPercent,
                    DiscountAmount = uiItem.DiscountAmount,
                    TaxableValue = uiItem.TaxableValue,
                    GSTPercentage = uiItem.GSTPercentage,
                    CGSTAmount = uiItem.CGSTAmount,
                    SGSTAmount = uiItem.SGSTAmount,
                    IGSTAmount = uiItem.IGSTAmount,
                    TotalAmount = uiItem.TotalAmount
                };
                invoice.InvoiceItems.Add(newItem);
                _dbContext.Entry(newItem).State = EntityState.Added;
            }

            // Create payment receipt if Cash or Bank immediately
            if (PaymentMethod == "Cash" || PaymentMethod == "Bank")
            {
                var newPayment = new Payment
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    PaymentDate = InvoiceDate,
                    AmountPaid = GrandTotal,
                    PaymentMode = PaymentMethod,
                    TransactionReference = "Immediate Billing Settlement"
                };
                invoice.Payments.Add(newPayment);
                _dbContext.Entry(newPayment).State = EntityState.Added;
            }

            // Deduct Stock levels and Log Stock ledger
            foreach (var item in invoice.InvoiceItems)
            {
                var product = await _dbContext.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.CurrentStock -= item.Quantity;
                    _dbContext.Products.Update(product);

                    // Add Stock ledger row
                    await _dbContext.Stocks.AddAsync(new Stock
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        TransactionType = "Sale",
                        Quantity = -item.Quantity,
                        ReferenceId = invoice.Id,
                        Remarks = $"Sale Invoice {formattedNumToSave}"
                    });
                }
            }

            // Adjust Customer Outstanding balance if Credit billing
            if (PaymentMethod == "Credit")
            {
                var customer = await _dbContext.Customers.FindAsync(SelectedCustomer.Id);
                if (customer != null)
                {
                    customer.OutstandingBalance += GrandTotal;
                    _dbContext.Customers.Update(customer);
                }
            }

            // Save transaction to DB
            if (!IsEditMode)
            {
                await _invoiceRepository.AddAsync(invoice);
            }
            await _invoiceRepository.SaveAsync();

            await _auditLogger.LogActivityAsync(IsEditMode ? "Edit Invoice" : "Create Invoice", "Billing", $"Saved invoice {formattedNumToSave} for customer {SelectedCustomer.Name}");

            // Generate PDF and save to configured local storage path under user-specific subfolder
            string localFilePath = string.Empty;
            try
            {
                string username = _sessionContext.CurrentUser?.Username?.Trim()?.ToLowerInvariant() ?? "admin";
                string userFolder = (username.Contains("cashier") || username.Contains("samundeeswari")) ? "cashier" : "admin";
                
                string invoiceStorageDir = string.IsNullOrWhiteSpace(setting.InvoiceStorageFolderPath)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BillingSoftware", "Invoices")
                    : setting.InvoiceStorageFolderPath;

                string targetDir = Path.Combine(invoiceStorageDir, userFolder);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string pdfFileName = $"{formattedNumToSave!.Replace("/", "_").Replace("\\", "_")}.pdf";
                localFilePath = Path.Combine(targetDir, pdfFileName);

                var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.Id == _sessionContext.CurrentCompanyId) ?? new Company { Id = _sessionContext.CurrentCompanyId };
                var fullInvoice = await _dbContext.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id);
                    
                if (fullInvoice != null)
                {
                    BillingSoftware.Printing.InvoicePdfGenerator.GeneratePdf(fullInvoice, company, localFilePath);
                }
            }
            catch (Exception pdfEx)
            {
                System.Windows.MessageBox.Show("Invoice saved in database, but PDF file generation failed: " + pdfEx.Message, "PDF Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }

            // Print invoice if print request is active
            if (printInvoice && !string.IsNullOrEmpty(localFilePath) && File.Exists(localFilePath))
            {
                try
                {
                    // Send directly to printer
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var printProcess = new System.Diagnostics.Process
                            {
                                StartInfo = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = localFilePath,
                                    Verb = "print",
                                    CreateNoWindow = true,
                                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                                    UseShellExecute = true
                                }
                            };
                            printProcess.Start();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                // Fallback: Open PDF in default viewer (like Edge/Chrome)
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = localFilePath,
                                    UseShellExecute = true
                                });

                                System.Windows.MessageBox.Show("Direct silent printing is not supported by your system's default PDF configuration. We have opened the invoice in your default viewer so you can print it (Ctrl + P).", "Direct Print Fallback", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                            }
                            catch (Exception openEx)
                            {
                                System.Windows.MessageBox.Show($"Direct print command failed, and we could not open the PDF externally.\nDetails: {openEx.Message}", "Print Failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            }
                        }
                    });
                }
                catch (Exception printEx)
                {
                    System.Windows.MessageBox.Show("PDF generated, but printing command encountered an issue: " + printEx.Message, "Print Command Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }

            System.Windows.MessageBox.Show($"Invoice {formattedNumToSave} saved successfully.", "Invoice Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            
            // Clear form
            ClearForm();
        }
        catch (DbUpdateConcurrencyException dbEx)
        {
            var entry = dbEx.Entries.FirstOrDefault();
            var entityName = entry?.Entity.GetType().Name ?? "Unknown";
            var state = entry?.State.ToString() ?? "Unknown";
            System.Windows.MessageBox.Show($"Failed to save invoice due to concurrency error in {entityName} (State: {state}):\n{dbEx.Message}", "Database Concurrency Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to save invoice: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            SaveInvoiceCommand.NotifyCanExecuteChanged();
            PrintInvoiceCommand.NotifyCanExecuteChanged();
        }
    }

    private void ClearForm()
    {
        EditingInvoiceId = null;
        Title = "New Tax Invoice";
        SelectedCustomer = null;
        SelectedProduct = null;
        CustomerSearchText = string.Empty;
        ProductSearchText = string.Empty;
        InvoiceDate = DateTime.Today;
        BillingAddress = string.Empty;
        ShippingAddress = string.Empty;
        POInvoiceNumber = string.Empty;
        PODate = null;
        PaymentMethod = "Credit";
        InvoiceItems.Clear();
        
        SubTotal = 0;
        DiscountTotal = 0;
        CGSTTotal = 0;
        SGSTTotal = 0;
        IGSTTotal = 0;
        RoundOff = 0;
        GrandTotal = 0;
        AmountInWords = string.Empty;

        IsManualInvoiceNumber = false;
        _ = LoadNextInvoiceNumberAsync();
    }
}
