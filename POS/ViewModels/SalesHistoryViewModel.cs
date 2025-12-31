using Microsoft.EntityFrameworkCore;
using POS.Persistence.Context;
using POS.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace POS.ViewModels
{
    public class SalesHistoryViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _dbContext;

        // Main Search
        private string _invoiceSearchQuery;
        public string InvoiceSearchQuery
        {
            get => _invoiceSearchQuery;
            set
            {
                _invoiceSearchQuery = value;
                OnPropertyChanged(nameof(InvoiceSearchQuery));
            }
        }

        private string _cashierSearchQuery;
        public string CashierSearchQuery
        {
            get => _cashierSearchQuery;
            set
            {
                _cashierSearchQuery = value;
                OnPropertyChanged(nameof(CashierSearchQuery));
                ApplyFilters();
            }
        }

        // Statistics
        private int _totalInvoices;
        public int TotalInvoices
        {
            get => _totalInvoices;
            set
            {
                _totalInvoices = value;
                OnPropertyChanged(nameof(TotalInvoices));
            }
        }

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                _totalAmount = value;
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        private decimal _averageInvoice;
        public decimal AverageInvoice
        {
            get => _averageInvoice;
            set
            {
                _averageInvoice = value;
                OnPropertyChanged(nameof(AverageInvoice));
            }
        }

        private int _totalItems;
        public int TotalItems
        {
            get => _totalItems;
            set
            {
                _totalItems = value;
                OnPropertyChanged(nameof(TotalItems));
            }
        }

        private int _filteredCount;
        public int FilteredCount
        {
            get => _filteredCount;
            set
            {
                _filteredCount = value;
                OnPropertyChanged(nameof(FilteredCount));
            }
        }

        // Data Collections
        private ObservableCollection<Invoice> _invoicesList;
        public ObservableCollection<Invoice> InvoicesList
        {
            get => _invoicesList;
            set
            {
                _invoicesList = value;
                OnPropertyChanged(nameof(InvoicesList));
            }
        }

        private Invoice _selectedInvoice;
        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                _selectedInvoice = value;
                OnPropertyChanged(nameof(SelectedInvoice));
            }
        }

        // Filter Properties
        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged(nameof(StartDate));
                ApplyFilters();
            }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged(nameof(EndDate));
                ApplyFilters();
            }
        }

        private ObservableCollection<Warehouse> _warehouses;
        public ObservableCollection<Warehouse> Warehouses
        {
            get => _warehouses;
            set
            {
                _warehouses = value;
                OnPropertyChanged(nameof(Warehouses));
            }
        }

        private Warehouse _selectedWarehouse;
        public Warehouse SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                _selectedWarehouse = value;
                OnPropertyChanged(nameof(SelectedWarehouse));
                ApplyFilters();
            }
        }

        private ObservableCollection<string> _paymentMethods;
        public ObservableCollection<string> PaymentMethods
        {
            get => _paymentMethods;
            set
            {
                _paymentMethods = value;
                OnPropertyChanged(nameof(PaymentMethods));
            }
        }

        private string _selectedPaymentMethod;
        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                _selectedPaymentMethod = value;
                OnPropertyChanged(nameof(SelectedPaymentMethod));
                ApplyFilters();
            }
        }

        // Commands
        public ICommand SearchCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand ViewInvoiceCommand { get; private set; }
        public ICommand PrintInvoiceCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }
        
        // Commands للـ Code-behind القديم
        public ICommand EditCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand ViewCommand { get; private set; }
        public ICommand PrintCommand { get; private set; }

        // Constructor
        public SalesHistoryViewModel()
        {
            _dbContext = new AppDbContext();
            InitializeData();
            InitializeCommands();
        }

        // Initialization
        private void InitializeData()
        {
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            
            PaymentMethods = new ObservableCollection<string>
            {
                "الكل",
                "نقدي",
                "آجل",
                "بطاقة ائتمان",
                "محفظة الكترونية"
            };
            SelectedPaymentMethod = "الكل";

            LoadWarehouses();
            LoadInvoices();
        }

        private void InitializeCommands()
        {
            SearchCommand = new ViewModelCommand(_ => ExecuteSearch());
            RefreshCommand = new ViewModelCommand(_ => ExecuteRefresh());
            ClearFiltersCommand = new ViewModelCommand(_ => ExecuteClearFilters());
            ViewInvoiceCommand = new ViewModelCommand(param => ExecuteViewInvoice(param as Invoice));
            PrintInvoiceCommand = new ViewModelCommand(param => ExecutePrintInvoice(param as Invoice));
            ExportCommand = new ViewModelCommand(_ => ExecuteExport());
            
            // للـ Code-behind القديم
            EditCommand = new ViewModelCommand(_ => ExecuteEdit());
            DeleteCommand = new ViewModelCommand(_ => ExecuteDelete());
            ViewCommand = new ViewModelCommand(_ => ExecuteView());
            PrintCommand = new ViewModelCommand(_ => ExecutePrint());
        }

        // Data Loading
        private void LoadWarehouses()
        {
            try
            {
                var warehouses = _dbContext.Warehouses.ToList();
                Warehouses = new ObservableCollection<Warehouse>(warehouses);
                
                if (Warehouses.Any())
                {
                    var allWarehouse = new Warehouse { Id = 0, Name = "جميع المخازن" };
                    Warehouses.Insert(0, allWarehouse);
                    SelectedWarehouse = allWarehouse;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المخازن: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadInvoices()
        {
            try
            {
                var invoices = _dbContext.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.SaleProducts)
                    .OrderByDescending(i => i.Date)
                    .ToList();

                InvoicesList = new ObservableCollection<Invoice>(invoices);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفواتير: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Filtering
        private void ApplyFilters()
        {
            try
            {
                var allInvoices = _dbContext.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.SaleProducts)
                    .OrderByDescending(i => i.Date)
                    .AsQueryable();

                // Filter by Invoice Number
                if (!string.IsNullOrWhiteSpace(InvoiceSearchQuery))
                {
                    allInvoices = allInvoices.Where(i => i.Number.Contains(InvoiceSearchQuery));
                }

                // Filter by Cashier Name
                if (!string.IsNullOrWhiteSpace(CashierSearchQuery))
                {
                    allInvoices = allInvoices.Where(i => i.CashierName != null && i.CashierName.Contains(CashierSearchQuery));
                }

                // Filter by Date Range
                if (StartDate.HasValue)
                {
                    allInvoices = allInvoices.Where(i => i.Date >= StartDate.Value);
                }

                if (EndDate.HasValue)
                {
                    var endOfDay = EndDate.Value.Date.AddDays(1).AddTicks(-1);
                    allInvoices = allInvoices.Where(i => i.Date <= endOfDay);
                }

                // Filter by Warehouse
                if (SelectedWarehouse != null && SelectedWarehouse.Id > 0)
                {
                    allInvoices = allInvoices.Where(i => i.WarehouseId == SelectedWarehouse.Id);
                }

                // Filter by Payment Method
                if (!string.IsNullOrEmpty(SelectedPaymentMethod) && SelectedPaymentMethod != "الكل")
                {
                    allInvoices = allInvoices.Where(i => i.PaymentMethod == SelectedPaymentMethod);
                }

                var filteredList = allInvoices.ToList();
                InvoicesList = new ObservableCollection<Invoice>(filteredList);
                FilteredCount = filteredList.Count;

                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تطبيق الفلاتر: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            if (InvoicesList == null || !InvoicesList.Any())
            {
                TotalInvoices = 0;
                TotalAmount = 0;
                AverageInvoice = 0;
                TotalItems = 0;
                return;
            }

            TotalInvoices = InvoicesList.Count;
            TotalAmount = InvoicesList.Sum(i => i.TotalPrice);
            AverageInvoice = TotalInvoices > 0 ? TotalAmount / TotalInvoices : 0;
            TotalItems = (int)InvoicesList.Sum(i => i.SaleProducts?.Sum(sp => sp.Quantity) ?? 0);
        }

        // Command Execution
        private void ExecuteSearch()
        {
            ApplyFilters();
        }

        private void ExecuteRefresh()
        {
            LoadInvoices();
        }

        private void ExecuteClearFilters()
        {
            InvoiceSearchQuery = string.Empty;
            CashierSearchQuery = string.Empty;
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            SelectedPaymentMethod = "الكل";
            
            if (Warehouses != null && Warehouses.Any())
            {
                SelectedWarehouse = Warehouses.First();
            }

            ApplyFilters();
        }

        private void ExecuteViewInvoice(Invoice invoice)
        {
            if (invoice == null) return;

            try
            {
                var dialog = new POS.Dialogs.ViewEditInvoiceDialog();
                dialog.viewModel.InvoiceId = invoice.Id;
                dialog.ShowDialog();
                
                // Refresh after dialog closes
                LoadInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عرض الفاتورة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecutePrintInvoice(Invoice invoice)
        {
            if (invoice == null) return;

            try
            {
                PrintInvoice(invoice);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في طباعة الفاتورة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteExport()
        {
            try
            {
                if (InvoicesList == null || !InvoicesList.Any())
                {
                    MessageBox.Show("لا توجد فواتير للتصدير", "تنبيه", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    FileName = $"فواتير_المبيعات_{DateTime.Now:yyyy-MM-dd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var excelService = new POS.Infrustructure.Services.ExcelService();
                    
                    var exportData = InvoicesList.Select(i => new
                    {
                        رقم_الفاتورة = i.Number,
                        التاريخ = i.Date.ToString("yyyy-MM-dd"),
                        العميل = i.Customer?.Name ?? "غير محدد",
                        المبلغ_الإجمالي = i.TotalPrice,
                        طريقة_الدفع = i.PaymentMethod ?? "نقدي",
                        الحالة = i.Status ?? "مكتملة"
                    });

                    var excelBytes = await excelService.ExportToExcelAsync(exportData, "فواتير المبيعات");
                    System.IO.File.WriteAllBytes(saveFileDialog.FileName, excelBytes);
                    
                    MessageBox.Show("تم التصدير بنجاح!", "نجح", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Methods للـ Code-behind القديم
        private void ExecuteEdit()
        {
            if (SelectedInvoice != null)
            {
                ExecuteViewInvoice(SelectedInvoice);
            }
        }
        
        private void ExecuteDelete()
        {
            if (SelectedInvoice == null) return;
            
            var result = MessageBox.Show($"هل تريد حذف الفاتورة رقم: {SelectedInvoice.Number}؟", 
                "تأكيد الحذف", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Invoices.Remove(SelectedInvoice);
                    _dbContext.SaveChanges();
                    LoadInvoices();
                    MessageBox.Show("تم حذف الفاتورة بنجاح", "نجح", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف الفاتورة: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void ExecuteView()
        {
            if (SelectedInvoice != null)
            {
                ExecuteViewInvoice(SelectedInvoice);
            }
        }
        
        private void ExecutePrint()
        {
            if (SelectedInvoice != null)
            {
                ExecutePrintInvoice(SelectedInvoice);
            }
        }

        private void PrintInvoice(Invoice invoice)
        {
            // Create a FlowDocument for printing
            FlowDocument doc = new FlowDocument();
            doc.PageWidth = 300; // Receipt width (thermal printer)
            doc.PagePadding = new Thickness(10);
            doc.FontFamily = new FontFamily("Segoe UI");
            doc.FlowDirection = FlowDirection.RightToLeft;

            // Company/Store Header
            Paragraph header = new Paragraph();
            header.TextAlignment = TextAlignment.Center;
            header.FontSize = 16;
            header.FontWeight = FontWeights.Bold;
            header.Inlines.Add(new Run("نقطة البيع"));
            doc.Blocks.Add(header);

            // Separator
            doc.Blocks.Add(CreateSeparator());

            // Invoice Info
            Paragraph invoiceInfo = new Paragraph();
            invoiceInfo.FontSize = 11;
            invoiceInfo.Inlines.Add(new Run($"رقم الفاتورة: {invoice.Number}\n"));
            invoiceInfo.Inlines.Add(new Run($"التاريخ: {invoice.Date:yyyy/MM/dd}\n"));
            invoiceInfo.Inlines.Add(new Run($"الوقت: {invoice.Date:HH:mm:ss}\n"));
            if (invoice.Customer != null)
            {
                invoiceInfo.Inlines.Add(new Run($"العميل: {invoice.Customer.Name}"));
            }
            doc.Blocks.Add(invoiceInfo);

            // Separator
            doc.Blocks.Add(CreateSeparator());

            // Products Table Header
            Paragraph tableHeader = new Paragraph();
            tableHeader.FontSize = 10;
            tableHeader.FontWeight = FontWeights.Bold;
            tableHeader.Inlines.Add(new Run("المنتج".PadLeft(20)));
            tableHeader.Inlines.Add(new Run("الكمية".PadLeft(8)));
            tableHeader.Inlines.Add(new Run("السعر".PadLeft(10)));
            tableHeader.Inlines.Add(new Run("الإجمالي".PadLeft(10)));
            doc.Blocks.Add(tableHeader);

            doc.Blocks.Add(CreateSeparator());

            // Products List
            if (invoice.SaleProducts != null)
            {
                foreach (var item in invoice.SaleProducts)
                {
                    Paragraph productLine = new Paragraph();
                    productLine.FontSize = 10;
                    productLine.Margin = new Thickness(0, 2, 0, 2);

                    string productName = item.Product?.Name ?? "منتج";
                    if (productName.Length > 18)
                        productName = productName.Substring(0, 15) + "...";

                    productLine.Inlines.Add(new Run($"{productName}\n"));
                    productLine.Inlines.Add(new Run($"   {item.Quantity:0.##} × {item.SalePrice:F2} = {(item.Quantity * item.SalePrice):F2} جنيه"));
                    doc.Blocks.Add(productLine);
                }
            }

            // Separator
            doc.Blocks.Add(CreateSeparator());

            // Summary Section
            Paragraph summary = new Paragraph();
            summary.FontSize = 11;
            summary.Inlines.Add(new Run($"الإجمالي الفرعي: {invoice.Subtotal:F2} جنيه\n"));
            
            if (invoice.Tax > 0)
            {
                summary.Inlines.Add(new Run($"الضريبة ({invoice.Tax}%): {(invoice.Tax / 100) * invoice.Subtotal:F2} جنيه\n"));
            }
            if (invoice.Discount > 0)
            {
                summary.Inlines.Add(new Run($"الخصم: {invoice.Discount:F2} جنيه\n"));
            }
            doc.Blocks.Add(summary);

            // Total
            Paragraph totalPara = new Paragraph();
            totalPara.FontSize = 14;
            totalPara.FontWeight = FontWeights.Bold;
            totalPara.TextAlignment = TextAlignment.Center;
            totalPara.Inlines.Add(new Run($"الإجمالي: {invoice.TotalPrice:F2} جنيه"));
            doc.Blocks.Add(totalPara);

            // Separator
            doc.Blocks.Add(CreateSeparator());

            // Payment Info
            Paragraph paymentInfo = new Paragraph();
            paymentInfo.FontSize = 11;
            paymentInfo.Inlines.Add(new Run($"المبلغ المدفوع: {invoice.AmountPaid:F2} جنيه\n"));
            paymentInfo.Inlines.Add(new Run($"الباقي: {invoice.ChangeAmount:F2} جنيه\n"));
            paymentInfo.Inlines.Add(new Run($"طريقة الدفع: {invoice.PaymentMethod ?? "نقدي"}"));
            doc.Blocks.Add(paymentInfo);

            // Separator
            doc.Blocks.Add(CreateSeparator());

            // Footer
            Paragraph footer = new Paragraph();
            footer.TextAlignment = TextAlignment.Center;
            footer.FontSize = 10;
            footer.Inlines.Add(new Run("شكراً لتعاملكم معنا\n"));
            footer.Inlines.Add(new Run("نتمنى لكم يوماً سعيداً"));
            doc.Blocks.Add(footer);

            // Print the document
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource idpSource = doc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, $"فاتورة رقم {invoice.Number}");
                MessageBox.Show("تمت الطباعة بنجاح!", "طباعة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private Paragraph CreateSeparator()
        {
            Paragraph separator = new Paragraph();
            separator.FontSize = 10;
            separator.Inlines.Add(new Run("─────────────────────────────"));
            separator.Margin = new Thickness(0, 5, 0, 5);
            return separator;
        }

        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
