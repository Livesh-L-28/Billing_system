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

    private string _paymentMethod = "Cash"; // Cash, Credit, Bank
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

    // Commands
    public RelayCommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public IAsyncRelayCommand SaveInvoiceCommand { get; }
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
        ClearFormCommand = new RelayCommand(ClearForm);

        Title = "New Tax Invoice";
        _ = LoadLookupsAsync();
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            var custs = await _dbContext.Customers.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            Customers = new ObservableCollection<Customer>(custs);
            OnPropertyChanged(nameof(Customers));

            var prods = await _dbContext.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            Products = new ObservableCollection<Product>(prods);
            OnPropertyChanged(nameof(Products));

            await LoadNextInvoiceNumberAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to load selectors: " + ex.Message);
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
                var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Id == 1) ?? new Setting { Id = 1 };
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
                ProductName = SelectedProduct.Name,
                HSNCode = SelectedProduct.HSNCode,
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
        // Get company state
        var company = _dbContext.Companies.FirstOrDefault(c => c.Id == 1);
        string compState = company?.State ?? "Maharashtra";
        string custState = SelectedCustomer?.State ?? "Maharashtra";

        if (compState.Equals(custState, StringComparison.OrdinalIgnoreCase))
        {
            // Intra-state supply: Apply CGST + SGST (Half-half)
            decimal halfRate = item.GSTPercentage / 2;
            item.CGSTAmount = Math.Round(item.TaxableValue * (halfRate / 100), 2);
            item.SGSTAmount = Math.Round(item.TaxableValue * (halfRate / 100), 2);
            item.IGSTAmount = 0;
        }
        else
        {
            // Inter-state supply: Apply IGST (Full rate)
            item.CGSTAmount = 0;
            item.SGSTAmount = 0;
            item.IGSTAmount = Math.Round(item.TaxableValue * (item.GSTPercentage / 100), 2);
        }

        item.TotalAmount = item.TaxableValue + item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
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
    }

    private bool CanSaveInvoice()
    {
        return SelectedCustomer != null && InvoiceItems.Count > 0 && !IsBusy;
    }

    private async Task SaveInvoiceAsync()
    {
        if (SelectedCustomer == null) return;
        if (InvoiceItems.Count == 0) return;

        IsBusy = true;
        SaveInvoiceCommand.NotifyCanExecuteChanged();

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
            var setting = await _dbContext.Settings.FirstOrDefaultAsync(s => s.Id == 1) ?? new Setting { Id = 1 };
            
            int invoiceNumToSave;
            string formattedNumToSave;

            if (IsManualInvoiceNumber)
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

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = invoiceNumToSave,
                FormattedInvoiceNumber = formattedNumToSave,
                InvoiceDate = InvoiceDate,
                FinancialYearId = activeFY.Id,
                CustomerId = SelectedCustomer.Id,
                BillingAddress = BillingAddress,
                ShippingAddress = ShippingAddress,
                POInvoiceNumber = POInvoiceNumber,
                PODate = PODate,
                DueDate = DueDate,
                PaymentMethod = PaymentMethod,
                SubTotal = SubTotal,
                DiscountAmount = DiscountTotal,
                CGSTTotal = CGSTTotal,
                SGSTTotal = SGSTTotal,
                IGSTTotal = IGSTTotal,
                RoundOff = RoundOff,
                GrandTotal = GrandTotal,
                IsCancelled = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _sessionContext.CurrentUser?.Id ?? Guid.Empty
            };

            // Copy items to invoice entity
            foreach (var uiItem in InvoiceItems)
            {
                invoice.InvoiceItems.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ProductId = uiItem.ProductId,
                    ProductName = uiItem.ProductName,
                    HSNCode = uiItem.HSNCode,
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
                });
            }

            // Create payment receipt if Cash or Bank immediately
            if (PaymentMethod == "Cash" || PaymentMethod == "Bank")
            {
                invoice.Payments.Add(new Payment
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    PaymentDate = InvoiceDate,
                    AmountPaid = GrandTotal,
                    PaymentMode = PaymentMethod,
                    TransactionReference = "Immediate Billing Settlement"
                });
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
            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveAsync();

            await _auditLogger.LogActivityAsync("Create Invoice", "Billing", $"Created invoice {formattedNumToSave} for customer {SelectedCustomer.Name}");

            // Generate PDF & Show Print Preview
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "BillingSoftware");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                var tempFile = Path.Combine(tempDir, $"{formattedNumToSave.Replace("/", "_").Replace("\\", "_")}.pdf");
                
                var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.Id == 1) ?? new Company { Id = 1 };
                var fullInvoice = await _dbContext.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.InvoiceItems)
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id);
                    
                if (fullInvoice != null)
                {
                    BillingSoftware.Printing.InvoicePdfGenerator.GeneratePdf(fullInvoice, company, tempFile);
                    
                    // Show Print Preview
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var previewWin = new Views.PrintPreviewWindow(tempFile);
                        
                        // Safely assign active workspace window as owner
                        var activeWindow = System.Windows.Application.Current.Windows
                            .OfType<System.Windows.Window>()
                            .FirstOrDefault(w => w.IsActive || w.GetType().Name == "MainWindow");
                            
                        if (activeWindow != null && activeWindow != previewWin)
                        {
                            previewWin.Owner = activeWindow;
                        }
                        
                        previewWin.ShowDialog();
                    });
                }
            }
            catch (Exception pdfEx)
            {
                System.Windows.MessageBox.Show("Invoice saved, but PDF print generation failed: " + pdfEx.Message, "PDF Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }

            System.Windows.MessageBox.Show($"Invoice {formattedNumToSave} saved successfully.", "Invoice Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            
            // Clear form
            ClearForm();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to save invoice: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            SaveInvoiceCommand.NotifyCanExecuteChanged();
        }
    }

    private void ClearForm()
    {
        SelectedCustomer = null;
        SelectedProduct = null;
        InvoiceDate = DateTime.Today;
        BillingAddress = string.Empty;
        ShippingAddress = string.Empty;
        POInvoiceNumber = string.Empty;
        PODate = null;
        PaymentMethod = "Cash";
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
