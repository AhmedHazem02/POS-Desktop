using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Contracts.Services;
using POS.Domain.Models.Payments.PaymentMethods;
using POS.Domain.Models;
using POS.Domain.Models.Payments;
using POS.Domain.Models.Products;
using POS.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace POS.ViewModels
{
    public class POSViewModel : BaseProductsViewModel
    {
        private readonly ICustomerLedgerService _ledgerService;
        private DateTime _purchaseDate = DateTime.Now;
        private DateTime _paymentDate = DateTime.Now;
        public DateTime PurchaseDate
        {
            get { return _purchaseDate; }
            set
            {
                if (_purchaseDate != value)
                {
                    _purchaseDate = value;
                    OnPropertyChanged(nameof(PurchaseDate));
                    OnPropertyChanged(nameof(InvoiceDate));
                }
            }
        }

        public DateTime PaymentDate
        {
            get { return _paymentDate; }
            set
            {
                if (_paymentDate != value)
                {
                    _paymentDate = value;
                    OnPropertyChanged(nameof(PaymentDate));
                }
            }
        }

        public DateTime InvoiceDate
        {
            get => PurchaseDate;
            set
            {
                if (PurchaseDate != value)
                {
                    PurchaseDate = value;
                    OnPropertyChanged(nameof(InvoiceDate));
                }
            }
        }
        public string GetNextInvoiceNumber(bool filterByCurrentDay)
        {
            string invoiceNumberPrefix = "INV-"; // Prefix for the invoice number

            // Get the highest invoice number
            string highestInvoiceNumber = _dbContext.Invoices
                .Where(i => !filterByCurrentDay || EF.Functions.Like(i.Number, $"{invoiceNumberPrefix}%")) // Filter by current day if required
                .Select(i => i.Number)
                .OrderByDescending(n => n)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(highestInvoiceNumber)) // If no invoice numbers found
            {
                return $"{invoiceNumberPrefix}001"; // Start with 001 for the current day
            }
            else
            {
                // Extract the numeric part of the highest invoice number
                string numericPart = highestInvoiceNumber.Substring(invoiceNumberPrefix.Length);
                int nextNumber = int.Parse(numericPart) + 1; // Increment the number

                return $"{invoiceNumberPrefix}{nextNumber:D3}"; // Format the next invoice number
            }
        }

        private string _billNumber;
        public string BillNumber
        {
            get { return _billNumber; }
            set
            {
                if (_billNumber != value)
                {
                    _billNumber = value;
                    OnPropertyChanged(nameof(BillNumber));
                    OnPropertyChanged(nameof(InvoiceNumber));
                }
            }
        }

        public string InvoiceNumber
        {
            get => BillNumber;
            set
            {
                if (BillNumber != value)
                {
                    BillNumber = value;
                    OnPropertyChanged(nameof(InvoiceNumber));
                }
            }
        }

        private string _cashierName;
        public string CashierName
        {
            get => _cashierName;
            set
            {
                if (_cashierName != value)
                {
                    _cashierName = value;
                    OnPropertyChanged(nameof(CashierName));
                }
            }
        }

        private string _cashierImagePath = "/Assets/pic/default-avatar.png";
        public string CashierImagePath
        {
            get => _cashierImagePath;
            set
            {
                if (_cashierImagePath != value)
                {
                    _cashierImagePath = value;
                    OnPropertyChanged(nameof(CashierImagePath));
                }
            }
        }

        private string _barcodeSearchText;
        public string BarcodeSearchText
        {
            get => _barcodeSearchText;
            set
            {
                if (_barcodeSearchText != value)
                {
                    _barcodeSearchText = value;
                    OnPropertyChanged(nameof(BarcodeSearchText));
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        SearchProductByBarcode(value);
                    }
                }
            }
        }

        private string _scannedProductName = "لا يوجد منتج";
        public string ScannedProductName
        {
            get => _scannedProductName;
            set
            {
                if (_scannedProductName != value)
                {
                    _scannedProductName = value;
                    OnPropertyChanged(nameof(ScannedProductName));
                }
            }
        }

        private double _scannedProductPrice;
        public double ScannedProductPrice
        {
            get => _scannedProductPrice;
            set
            {
                if (_scannedProductPrice != value)
                {
                    _scannedProductPrice = value;
                    OnPropertyChanged(nameof(ScannedProductPrice));
                }
            }
        }

        private double _scannedProductStock;
        public double ScannedProductStock
        {
            get => _scannedProductStock;
            set
            {
                if (_scannedProductStock != value)
                {
                    _scannedProductStock = value;
                    OnPropertyChanged(nameof(ScannedProductStock));
                }
            }
        }

        private ObservableCollection<Customer> _topCustomers;
        public ObservableCollection<Customer> TopCustomers
        {
            get => _topCustomers;
            set
            {
                if (_topCustomers != value)
                {
                    _topCustomers = value;
                    OnPropertyChanged(nameof(TopCustomers));
                }
            }
        }

        public ICommand SearchProductCommand { get; private set; }
        public ICommand SelectCustomerCommand { get; private set; }
        public ICommand CancelInvoiceCommand { get; private set; }
        
        public double TaxAmount => (Tax / 100) * SubTotal;
        public double DiscountAmount => Discount;
        
        private double _paidAmount;
        public double PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (Math.Abs(_paidAmount - value) > 0.01)
                {
                    _paidAmount = value;
                    OnPropertyChanged(nameof(PaidAmount));
                    OnPropertyChanged(nameof(RemainingAmount));
                }
            }
        }
        
        public double RemainingAmount => Math.Max(0, TotalAmount - PaidAmount);
        
        private ObservableCollection<SaleProduct> _cartItemsList;

        public ObservableCollection<SaleProduct> CartItemsList
        {
            get { return _cartItemsList; }
            set
            {
                if (_cartItemsList != value)
                {
                    _cartItemsList = value;
                    OnPropertyChanged(nameof(CartItemsList));

                }
            }
        }

        public int CartItemsCount => CartItemsList?.Count ?? 0;

        private SaleProduct _selectedCartItem;

        public SaleProduct SelectedCartItem
        {
            get { return _selectedCartItem; }
            set
            {
                if (_selectedCartItem != value)
                {
                    _selectedCartItem = value;
                    OnPropertyChanged(nameof(SelectedCartItem));
                    SetSelectedItemValues(SelectedCartItem);
                }
            }
        }

        private const double ChangeTolerance = 0.001;
        private double _amountPaid;
        private double _manualAmount;
        private string _billBreakdown;
        private ObservableCollection<CashBillItem> _cashBills;
        private bool _isCashPaymentSelected = true;

        public ObservableCollection<CashBillItem> CashBills
        {
            get => _cashBills;
            set
            {
                if (_cashBills != value)
                {
                    _cashBills = value;
                    OnPropertyChanged(nameof(CashBills));
                }
            }
        }

        public bool IsCashPaymentSelected
        {
            get => _isCashPaymentSelected;
            set
            {
                if (_isCashPaymentSelected != value)
                {
                    _isCashPaymentSelected = value;
                    OnPropertyChanged(nameof(IsCashPaymentSelected));
                }
            }
        }

        public Dictionary<double, int> SelectedBills { get; } = new Dictionary<double, int>();

        public double ManualAmount
        {
            get => _manualAmount;
            set
            {
                if (Math.Abs(_manualAmount - value) > ChangeTolerance)
                {
                    _manualAmount = value;
                    OnPropertyChanged(nameof(ManualAmount));
                    RecalculatePayment();
                }
            }
        }

        public double AmountPaid
        {
            get => _amountPaid;
            private set
            {
                if (Math.Abs(_amountPaid - value) > ChangeTolerance)
                {
                    _amountPaid = value;
                    OnPropertyChanged(nameof(AmountPaid));
                    RefreshPaymentState();
                }
            }
        }

        public double Change => AmountPaid - TotalAmount;

        public bool IsPaymentComplete => AmountPaid >= TotalAmount;

        public bool CanCompletePayment => IsPaymentComplete || (SelectedCustomer != null && !_ledgerService.IsDefaultCustomer(SelectedCustomer));

        public bool IsChangePositive => Change > ChangeTolerance;
        public bool IsChangeNegative => Change < -ChangeTolerance;
        public bool IsChangeExact => !IsChangePositive && !IsChangeNegative;

        public string ChangeDisplay
        {
            get
            {
                if (IsChangeNegative)
                {
                    return $"عليك {Math.Abs(Change):F2} جنيه";
                }

                return $"{Change:F2} جنيه";
            }
        }

        public string BillBreakdown
        {
            get => _billBreakdown;
            private set
            {
                if (_billBreakdown != value)
                {
                    _billBreakdown = value;
                    OnPropertyChanged(nameof(BillBreakdown));
                }
            }
        }
        private void RecalculateTotalAmount()
        {
            TotalQuantity = CartItemsList.Sum(item => item.Quantity);
            SubTotal = CartItemsList.Sum(item => item.SalePrice * item.Quantity);
            TotalAmount = SubTotal + ((Tax / 100) * SubTotal) - Discount;
            // Earnings = (double)(TotalAmount - CartItemsList.Sum(item => item.Product.GetLastPurchasePrice() * item.Quantity));
            OnPropertyChanged(nameof(CartItemsCount));
            RefreshPaymentState();
        }
        public void SetSelectedItemValues(SaleProduct selectedCartItem)
        {
            if (selectedCartItem != null)
            {
                SelectedProduct = ProductList.FirstOrDefault(product => product.Id == selectedCartItem.ProductId && selectedCartItem.WarehouseId == (SelectedWarehouse?.Id ?? 0));
                //SelectedCategory = CategoryList.FirstOrDefault(category => category.Name == selectedCartItem.Category);

                ItemName = selectedCartItem.Product.Name;
                ItemBarcode = selectedCartItem.Product.Barcode;
                Quantity = selectedCartItem.Quantity;
                SalePrice = selectedCartItem.SalePrice;
                Notes = selectedCartItem.Details;
                ProductWarehouse = selectedCartItem.Warehouse;
                SelectedReadyProduct = selectedCartItem.ReadyProduct;
            }
        }

        private void CartItemsList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SaleProduct item in e.NewItems)
                {
                    item.PropertyChanged += CartItem_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (SaleProduct item in e.OldItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                }
            }

            RecalculateTotalAmount();
        }

        private void CartItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SaleProduct.Quantity) || e.PropertyName == nameof(SaleProduct.SalePrice))
            {
                RecalculateTotalAmount();
            }
        }
        public ICommand AddCommand { get; }
        public ICommand AcceptCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PaymentCommand { get; }
        public ICommand DeliveryCommand { get; }
        public ICommand SuspendBillCommand { get; }
        public ICommand CancelBillCommand { get; }
        public ICommand AddBillCommand { get; }
        public ICommand ClearPaymentCommand { get; }
        public ICommand CompletePaymentCommand { get; }
        public ICommand CancelSaleCommand { get; }
        public ICommand RemoveCartItemCommand { get; }
        public ICommand IncreaseCartItemCommand { get; }
        public ICommand DecreaseCartItemCommand { get; }
        public ICommand CloseWindowCommand { get; }
        public ICommand PrintInvoiceCommand { get; }
        public POSViewModel() : base()
        {
            _ledgerService = App.ServiceProvider.GetRequiredService<ICustomerLedgerService>();
            BillNumber = GetNextInvoiceNumber(false);
            CartItemsList = new ObservableCollection<SaleProduct>();
            CartItemsList.CollectionChanged += CartItemsList_CollectionChanged;
            
            // تحميل معلومات الكاشير من المستخدم الحالي
            LoadCashierInfo();
            
            // تحميل أفضل العملاء
            LoadTopCustomers();
            
            InvoiceDate = DateTime.Now;
            CashBills = BuildCashBills();
            BillBreakdown = "لا توجد فئات مدفوعة";
            
            // Initialize Commands
            AddCommand = new RelayCommand(ExecuteAddCommand);
            AcceptCommand = new RelayCommand(ExecuteAcceptCommand);
            CancelCommand = new RelayCommand(ExecuteCancelCommand);
            DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
            PaymentCommand = new RelayCommand(ExecutePayment);
            DeliveryCommand = new RelayCommand(ExecuteDelivery);
            SuspendBillCommand = new RelayCommand(ExecuteSuspendBillCommand);
            CancelBillCommand = new RelayCommand(ExecuteCancelBill);
            AddBillCommand = new RelayCommand(ExecuteAddBillCommand);
            ClearPaymentCommand = new RelayCommand(ExecuteClearPaymentCommand);
            CompletePaymentCommand = new RelayCommand(ExecuteCompletePaymentCommand);
            CancelSaleCommand = new RelayCommand(ExecuteCancelSaleCommand);
            RemoveCartItemCommand = new RelayCommand(ExecuteRemoveCartItemCommand);
            IncreaseCartItemCommand = new RelayCommand(ExecuteIncreaseCartItemCommand);
            DecreaseCartItemCommand = new RelayCommand(ExecuteDecreaseCartItemCommand);
            CloseWindowCommand = new RelayCommand(ExecuteCloseWindowCommand);
            PrintInvoiceCommand = new RelayCommand(ExecutePrintInvoiceCommand);
            CancelInvoiceCommand = new RelayCommand(ExecuteCancelInvoice);
            SearchProductCommand = new RelayCommand(ExecuteSearchProduct);
            SelectCustomerCommand = new RelayCommand(ExecuteSelectCustomer);
            
            #region CartListEvents
            CartList_CurrentCellChangedCommand = new RelayCommand(ExecuteCartList_CurrentCellChangedCommand);
            CartList_SelectionChangedCommand = new RelayCommand(ExecuteCartList_SelectionChangedCommand);
            CartList_MouseDownCommand = new RelayCommand(ExecuteCartList_MouseDownCommand);
            CartList_MouseDoubleClickCommand = new RelayCommand(ExecuteCartList_MouseDoubleClickCommand);
            CartList_DataContextChangedCommand = new RelayCommand(ExecuteCartList_DataContextChangedCommand);
            CartList_SelectedCellsChangedCommand = new RelayCommand(ExecuteCartList_SelectedCellsChangedCommand);
            #endregion
            
            PropertyChanged += OnInvoiceDetailsChanged;
            IsSale = true;
        }
        private void OnInvoiceDetailsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tax) || e.PropertyName == nameof(Discount))
            {
                RecalculateTotalAmount();
            }

            if (e.PropertyName == nameof(SelectedCustomer))
            {
                OnPropertyChanged(nameof(CanCompletePayment));
            }
        }

        private ObservableCollection<CashBillItem> BuildCashBills()
        {
            return new ObservableCollection<CashBillItem>
            {
                new CashBillItem(200, "200", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"))),
                new CashBillItem(100, "100", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"))),
                new CashBillItem(50, "50", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6F00"))),
                new CashBillItem(20, "20", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#388E3C"))),
                new CashBillItem(10, "10", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"))),
                new CashBillItem(5, "5", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B1FA2"))),
                new CashBillItem(1, "1", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#546E7A"))),
                new CashBillItem(0.5, "0.50", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8D6E63")))
            };
        }

        private void RefreshPaymentState()
        {
            OnPropertyChanged(nameof(Change));
            OnPropertyChanged(nameof(IsPaymentComplete));
            OnPropertyChanged(nameof(CanCompletePayment));
            OnPropertyChanged(nameof(IsChangePositive));
            OnPropertyChanged(nameof(IsChangeNegative));
            OnPropertyChanged(nameof(IsChangeExact));
            OnPropertyChanged(nameof(ChangeDisplay));
        }

        private void UpdateSelectedBills()
        {
            SelectedBills.Clear();
            foreach (var bill in CashBills)
            {
                if (bill.Count > 0)
                {
                    SelectedBills[bill.Value] = bill.Count;
                }
            }
        }

        private void RecalculatePayment()
        {
            UpdateSelectedBills();
            double billsTotal = CashBills.Sum(bill => bill.Value * bill.Count);
            AmountPaid = billsTotal + ManualAmount;
            BillBreakdown = GenerateBillBreakdown();
        }

        private string GenerateBillBreakdown()
        {
            if (SelectedBills.Count == 0 && ManualAmount <= 0)
            {
                return "لا توجد فئات مدفوعة";
            }

            var sb = new StringBuilder();
            foreach (var bill in SelectedBills.OrderByDescending(x => x.Key))
            {
                double lineTotal = bill.Key * bill.Value;
                sb.AppendLine($"{bill.Value} x {bill.Key:0.##} جنيه = {lineTotal:0.##} جنيه");
            }

            if (ManualAmount > 0)
            {
                sb.AppendLine($"المبلغ اليدوي: {ManualAmount:0.##} جنيه");
            }

            sb.AppendLine("------------");
            sb.AppendLine($"الإجمالي المدفوع: {AmountPaid:0.##} جنيه");
            return sb.ToString().TrimEnd();
        }
        private void ExecuteAddCommand(object parameter)
        {
            if (SelectedProduct != null)
            {
                Quantity++;
            }
        }

        private void ExecuteAddBillCommand(object parameter)
        {
            if (parameter == null)
            {
                return;
            }

            if (!double.TryParse(parameter.ToString(), out double value))
            {
                return;
            }

            var bill = CashBills.FirstOrDefault(x => Math.Abs(x.Value - value) < ChangeTolerance);
            if (bill != null)
            {
                bill.Count++;
            }

            RecalculatePayment();
        }

        private void ExecuteClearPaymentCommand(object parameter)
        {
            ClearPayment();
        }

        private void ExecuteCompletePaymentCommand(object parameter)
        {
            if (CartItemsList.Count == 0)
            {
                MessageBox.Show("لا توجد عناصر في السلة.", "تنبيه");
                return;
            }

            if (!CanCompletePayment)
            {
                MessageBox.Show("لا يمكن إتمام البيع مع وجود باقي بدون اختيار عميل.", "تنبيه");
                return;
            }

            // تحديث المبلغ المدفوع
            PaidAmount = Math.Min(AmountPaid, TotalAmount);
            
            AddInvoiceWithSaleProducts();
            ClearPayment();
        }

        private void ExecuteCancelSaleCommand(object parameter)
        {
            CartItemsList.Clear();
            SelectedCartItem = null;
            Quantity = 0;
            SalePrice = 0;
            Notes = null;
            SelectedReadyProduct = null;
            RecalculateTotalAmount();
            ClearPayment();
            BillNumber = GetNextInvoiceNumber(false);
        }

        private void ExecuteRemoveCartItemCommand(object parameter)
        {
            if (parameter is SaleProduct item && CartItemsList.Contains(item))
            {
                CartItemsList.Remove(item);
            }
        }

        private void ExecuteIncreaseCartItemCommand(object parameter)
        {
            UpdateCartItemQuantity(parameter as SaleProduct, 1);
        }

        private void ExecuteDecreaseCartItemCommand(object parameter)
        {
            UpdateCartItemQuantity(parameter as SaleProduct, -1);
        }

        private void UpdateCartItemQuantity(SaleProduct item, double delta)
        {
            if (item == null)
            {
                return;
            }

            double newQuantity = item.Quantity + delta;
            if (newQuantity <= 0)
            {
                if (CartItemsList.Contains(item))
                {
                    CartItemsList.Remove(item);
                }
                return;
            }

            item.Quantity = newQuantity;
        }

        private void ExecuteCloseWindowCommand(object parameter)
        {
            var activeWindow = global::System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
            activeWindow?.Close();
        }

        private void ExecutePrintInvoiceCommand(object parameter)
        {
            if (CartItemsList == null || CartItemsList.Count == 0)
            {
                MessageBox.Show("لا توجد عناصر في السلة للطباعة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                PrintInvoice();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintInvoice()
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
            invoiceInfo.Inlines.Add(new Run($"رقم الفاتورة: {InvoiceNumber}\n"));
            invoiceInfo.Inlines.Add(new Run($"التاريخ: {InvoiceDate:yyyy/MM/dd}\n"));
            invoiceInfo.Inlines.Add(new Run($"الوقت: {DateTime.Now:HH:mm:ss}\n"));
            invoiceInfo.Inlines.Add(new Run($"الكاشير: {CashierName}"));
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
            foreach (var item in CartItemsList)
            {
                Paragraph productLine = new Paragraph();
                productLine.FontSize = 10;
                productLine.Margin = new Thickness(0, 2, 0, 2);

                string productName = item.Product?.Name ?? "منتج";
                if (productName.Length > 18)
                    productName = productName.Substring(0, 15) + "...";

                productLine.Inlines.Add(new Run($"{productName}\n"));
                productLine.Inlines.Add(new Run($"   {item.Quantity:0.##} × {item.SalePrice:F2} = {item.Total:F2} جنيه"));
                doc.Blocks.Add(productLine);
            }

            // Separator
            doc.Blocks.Add(CreateSeparator());

            // Summary Section
            Paragraph summary = new Paragraph();
            summary.FontSize = 11;
            summary.Inlines.Add(new Run($"عدد الأصناف: {CartItemsCount}\n"));
            summary.Inlines.Add(new Run($"الكمية الإجمالية: {TotalQuantity:0.##}\n"));

            if (Tax > 0)
            {
                summary.Inlines.Add(new Run($"الضريبة ({Tax}%): {(Tax / 100) * SubTotal:F2} جنيه\n"));
            }
            if (Discount > 0)
            {
                summary.Inlines.Add(new Run($"الخصم: {Discount:F2} جنيه\n"));
            }
            doc.Blocks.Add(summary);

            // Total
            Paragraph totalPara = new Paragraph();
            totalPara.FontSize = 14;
            totalPara.FontWeight = FontWeights.Bold;
            totalPara.TextAlignment = TextAlignment.Center;
            totalPara.Inlines.Add(new Run($"الإجمالي: {TotalAmount:F2} جنيه"));
            doc.Blocks.Add(totalPara);

            // Separator
            doc.Blocks.Add(CreateSeparator());

            // Payment Info
            Paragraph paymentInfo = new Paragraph();
            paymentInfo.FontSize = 11;
            paymentInfo.Inlines.Add(new Run($"المبلغ المدفوع: {AmountPaid:F2} جنيه\n"));

            if (Change >= 0)
            {
                paymentInfo.Inlines.Add(new Run($"الباقي: {Change:F2} جنيه"));
            }
            else
            {
                paymentInfo.Inlines.Add(new Run($"المتبقي عليك: {Math.Abs(Change):F2} جنيه"));
            }
            doc.Blocks.Add(paymentInfo);

            // Bill Breakdown (if bills were selected)
            if (!string.IsNullOrEmpty(BillBreakdown) && BillBreakdown != "لا توجد فئات مدفوعة")
            {
                doc.Blocks.Add(CreateSeparator());
                Paragraph billBreakdownPara = new Paragraph();
                billBreakdownPara.FontSize = 9;
                billBreakdownPara.Inlines.Add(new Run("الفئات المدفوعة:\n"));
                billBreakdownPara.Inlines.Add(new Run(BillBreakdown));
                doc.Blocks.Add(billBreakdownPara);
            }

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
                printDialog.PrintDocument(idpSource.DocumentPaginator, $"فاتورة رقم {InvoiceNumber}");
                MessageBox.Show("تمت الطباعة بنجاح!", "طباعة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private Paragraph CreateSeparator()
        {
            Paragraph separator = new Paragraph();
            separator.FontSize = 10;
            separator.Inlines.Add(new Run("-----------------------------"));
            separator.Margin = new Thickness(0, 5, 0, 5);
            return separator;
        }

        private void ExecuteCancelInvoice(object parameter)
        {
            if (CartItemsList == null || CartItemsList.Count == 0)
            {
                MessageBox.Show("لا توجد عناصر في الفاتورة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "هل أنت متأكد من إلغاء الفاتورة الحالية؟ سيتم حذف جميع المنتجات.",
                "تأكيد الإلغاء",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CartItemsList.Clear();
                ClearPayment();
                SelectedCustomer = null;
                InvoiceNumber = GetNextInvoiceNumber(false);
                OnPropertyChanged(nameof(TaxAmount));
                OnPropertyChanged(nameof(DiscountAmount));
                OnPropertyChanged(nameof(RemainingAmount));
                MessageBox.Show("تم إلغاء الفاتورة بنجاح.", "إلغاء", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearPayment()
        {
            foreach (var bill in CashBills)
            {
                bill.Count = 0;
            }

            ManualAmount = 0;
            AmountPaid = 0;
            SelectedBills.Clear();
            BillBreakdown = "لا توجد فئات مدفوعة";
            RefreshPaymentState();
        }

        private void ExecuteAcceptCommand(object parameter)
        {
            if (SelectedProduct != null && Quantity > 0 && SalePrice > 0)
            {
                // التحقق من وجود مخزن محدد
                if (SelectedWarehouse == null)
                {
                    MessageBox.Show("برجاء اختيار المخزن أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if the item already exists in the cart
                SaleProduct existingCartItem = CartItemsList.FirstOrDefault(item => item.ProductId == SelectedProduct.Id && item.WarehouseId == SelectedWarehouse.Id);

                if (existingCartItem != null)
                {
                    if (SelectedProduct.MinStock > 0)
                    {

                        double availableQuantity = (double)(SelectedProduct.MinStock - existingCartItem.Quantity);
                        if (availableQuantity > 0 && Quantity < availableQuantity)
                        {
                            MessageBoxResult result = MessageBox.Show($"الكمية المطلوبة أقل من الحد الأدنى للمخزون. هل تريد المتابعة؟", "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (result == MessageBoxResult.Yes)
                            {
                                existingCartItem.Quantity += Quantity;
                            }
                            else
                            {

                                MessageBox.Show("تم الإلغاء.");
                                return; // Exit the method
                            }

                        }
                    }
                    if (existingCartItem.Quantity + Quantity <= SelectedProduct.Quantity(SelectedWarehouse.Id))
                    {
                        existingCartItem.Quantity += Quantity;
                    }
                    else
                    {
                        // The sum exceeds the maximum quantity, set the quantity to the maximum
                        MessageBox.Show("الكمية المطلوبة تتجاوز الكمية المتاحة. تم تعيين الكمية إلى الحد الأقصى.");
                        existingCartItem.Quantity = SelectedProduct.Quantity(SelectedWarehouse.Id);
                    }
                    //existingCartItem.Earned = (double)((existingCartItem.SalePrice * existingCartItem.Quantity) - (existingCartItem.Product.GetLastPurchasePrice() * existingCartItem.Quantity));
                }
                else
                {
                    CartItemsList.Add(new SaleProduct
                    {
                        ProductId = SelectedProduct.Id,
                        Product = SelectedProduct,
                        Quantity = Quantity,
                        SalePrice = SalePrice,
                        CreatedDate = DateTime.Now,
                        Warehouse = SelectedWarehouse,
                        //Earned = (double)(SalePrice - (SelectedProduct.GetLastPurchasePrice() * Quantity)),
                        Details = Notes
                    }); ;
                }
                RecalculateTotalAmount();

                //Quantity = 1;
                //SalePrice = SelectedProduct.Price; // You may need to adjust this based on your requirements

            }

        }


        private void ExecuteCancelCommand(object parameter)
        {
            // Add logic for CancelCommand
        }

        private void ExecuteDeleteCommand(object parameter)
        {
            // Add logic for DeleteCommand
        }
        private void ExecutePayment(object obj)
        {
            // Create and show the payment dialog
            PaymentDialog paymentDialog = new PaymentDialog();
            paymentDialog.viewModel.Total = 100.ToString();
            paymentDialog.viewModel.TotalQuantity = 100.ToString();
            // Show the dialog as a modal window
            bool? result = paymentDialog.ShowDialog();

            // Check the result of the dialog
            if (result.HasValue)
            {
                // Payment dialog was closed, handle the result
                if (paymentDialog.viewModel.PaymentResult == true)
                {
                    AddInvoiceWithSaleProducts();

                }

            }
        }

        private void ExecuteSuspendBillCommand(object parameter)
        {
            // Handle logic for suspending bill
        }





        private void ExecuteDelivery(object obj)
        {
            // Put your delivery logic here
            Console.WriteLine("Delivery command executed");
        }

        private void ExecuteCancelBill(object obj)
        {
            // Put your cancel bill logic here
            Console.WriteLine("Cancel bill command executed");
        }

        /// <summary>
        /// تحميل معلومات الكاشير الحالي من نظام المصادقة
        /// </summary>
        private void LoadCashierInfo()
        {
            try
            {
                var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent();
                CashierName = currentUser?.Name?.Split('\\').LastOrDefault() ?? "الكاشير";
                // يمكن تحميل الصورة من قاعدة البيانات بناءً على اسم المستخدم
                CashierImagePath = "/Assets/pic/default-avatar.png";
            }
            catch
            {
                CashierName = "الكاشير";
                CashierImagePath = "/Assets/pic/default-avatar.png";
            }
        }

        /// <summary>
        /// تحميل أفضل العملاء من قاعدة البيانات (الأكثر شراءً)
        /// </summary>
        private async void LoadTopCustomers()
        {
            try
            {
                var customers = await _dbContext.Customers
                    .OrderByDescending(c => c.Invoices.Count)
                    .Take(5)
                    .ToListAsync();
                
                TopCustomers = new ObservableCollection<Customer>(customers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading customers: {ex.Message}");
                TopCustomers = new ObservableCollection<Customer>();
            }
        }

        /// <summary>
        /// البحث عن منتج بواسطة الباركود أو الاسم
        /// </summary>
        private async void SearchProductByBarcode(string searchText)
        {
            try
            {
                var product = await _dbContext.Products
                    .Include(p => p.PurchaseProducts)
                    .FirstOrDefaultAsync(p => p.Barcode == searchText || p.Name.Contains(searchText));

                if (product != null)
                {
                    ScannedProductName = product.Name;
                    ScannedProductPrice = product.SalePrice;
                    
                    // حساب الكمية المتبقية من آخر عملية شراء
                    var latestPurchase = product.PurchaseProducts?.OrderByDescending(p => p.PurchaseId).FirstOrDefault();
                    ScannedProductStock = latestPurchase?.Quantity ?? 0;

                    // إضافة المنتج للسلة تلقائياً
                    SelectedProduct = product;
                    ExecuteAcceptCommand(null);
                }
                else
                {
                    ScannedProductName = "لم يتم العثور على المنتج";
                    ScannedProductPrice = 0;
                    ScannedProductStock = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching product: {ex.Message}");
                ScannedProductName = "خطأ في البحث";
                ScannedProductPrice = 0;
                ScannedProductStock = 0;
            }
        }

        /// <summary>
        /// تنفيذ أمر البحث عن منتج
        /// </summary>
        private void ExecuteSearchProduct(object obj)
        {
            if (!string.IsNullOrWhiteSpace(BarcodeSearchText))
            {
                SearchProductByBarcode(BarcodeSearchText);
            }
        }

        /// <summary>
        /// تحديد العميل من القائمة
        /// </summary>
        private void ExecuteSelectCustomer(object obj)
        {
            if (obj is Customer customer)
            {
                SelectedCustomer = customer;
                CustomerSearchText = customer.Name;
            }
        }

        private void AddInvoiceWithSaleProducts()
        {
            var totalAmount = (decimal)TotalAmount;
            var paidAmount = (decimal)Math.Min(AmountPaid, TotalAmount);
            var invoicePaymentMethod = totalAmount - paidAmount > 0 ? "آجل" : "نقدي";
            var paymentEntryMethod = paidAmount > 0 ? "نقدي" : null;
            var customerId = SelectedCustomer?.Id;

            Invoice newInvoice = new Invoice
            {
                Number = BillNumber,
                Date = PurchaseDate,
                DueDate = PaymentDate,
                Tax = (decimal?)Tax,
                Discount = (decimal?)Discount,
                TotalPrice = totalAmount,
                Subtotal = (decimal)SubTotal,
                CashierName = CashierName,
                AmountPaid = paidAmount,
                ChangeAmount = (decimal)Change,
                PaymentMethod = invoicePaymentMethod,
                BillBreakdown = BillBreakdown,
                Status = invoicePaymentMethod == "آجل" ? "غير مكتملة" : "مكتملة",
                CustomerId = customerId
            };

            // Add the new invoice to the database
            _dbContext.Invoices.Add(newInvoice);
            _dbContext.SaveChanges(); // Save changes to generate the Invoice Id

            foreach (var cartItem in CartItemsList)
            {
                SaleProduct saleProduct = new SaleProduct
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    SalePrice = cartItem.SalePrice,
                    Warehouse = SelectedWarehouse,
                    Date = DateTime.Now,
                    // Earned = cartItem.Earned,
                    Details = cartItem.Details
                };
                saleProduct.InvoiceId = newInvoice.Id;
                _dbContext.SaleProducts.Add(saleProduct);
                _dbContext.SaveChanges();
            }

            if (paidAmount > 0)
            {
                var cashId = _ledgerService.RecordCashMovement(paidAmount, "POS", TransactionType.Income);
                _dbContext.InvoicePayments.Add(new InvoicePayment
                {
                    InvoiceId = newInvoice.Id,
                    Amount = paidAmount,
                    Date = DateTime.Now,
                    PaymentType = PaymentType.Cash,
                    CashId = cashId
                });
                _dbContext.SaveChanges();
            }

            if (customerId.HasValue)
            {
                _ledgerService.RecordInvoiceEntries(customerId.Value, newInvoice.Number, newInvoice.Date, totalAmount, paidAmount, invoicePaymentMethod, paymentEntryMethod);
            }

            // Clear the cart items list
            CartItemsList.Clear();

            // Reset selected cart item
            SelectedCartItem = null;

            // Reset other properties
            Quantity = 0;
            SalePrice = 0;
            Notes = null;
            SelectedReadyProduct = null;
            RecalculateTotalAmount();

            // Optionally, update the bill number for the next invoice
            BillNumber = GetNextInvoiceNumber(false);
        }

        #region CartListEvents
        private void ExecuteCartList_CurrentCellChangedCommand(object parameter)
        {
            // Handle CartList_CurrentCellChangedCommand logic here
        }

        private void ExecuteCartList_SelectionChangedCommand(object parameter)
        {
            // Handle CartList_SelectionChangedCommand logic here
        }

        private void ExecuteCartList_MouseDownCommand(object parameter)
        {
            // Handle CartList_MouseDownCommand logic here
        }

        private void ExecuteCartList_MouseDoubleClickCommand(object parameter)
        {
            // Handle CartList_MouseDoubleClickCommand logic here
        }

        private void ExecuteCartList_DataContextChangedCommand(object parameter)
        {
            // Handle CartList_DataContextChangedCommand logic here
        }

        private void ExecuteCartList_SelectedCellsChangedCommand(object parameter)
        {
            // Handle CartList_SelectedCellsChangedCommand logic here
        }



        #endregion

    }
}












