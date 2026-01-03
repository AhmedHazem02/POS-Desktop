using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Contracts.Services;
using POS.Domain.Models;
using POS.Persistence.Context;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace POS.ViewModels
{
    public class CustomerTransaction : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }
        public string Number { get; set; } = "";
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining => Amount - Paid;
        public string Status { get; set; } = "";
        public string Type { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
    }

    public class CustomersPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly ICustomerLedgerService _ledgerService;

        #region Properties
        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    LoadCustomers();
                }
            }
        }

        private ObservableCollection<Customer> _customers = new();
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set { _customers = value; OnPropertyChanged(nameof(Customers)); OnPropertyChanged(nameof(HasCustomers)); OnPropertyChanged(nameof(ShowEmptyCustomers)); }
        }

        public bool HasCustomers => Customers.Count > 0;

        private Customer? _selectedCustomer;
        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (_selectedCustomer != value)
                {
                    _selectedCustomer = value;
                    OnPropertyChanged(nameof(SelectedCustomer));
                    OnPropertyChanged(nameof(HasSelectedCustomer));
                    OnPropertyChanged(nameof(ShowEmptyTransactions));
                    if (_selectedCustomer != null)
                    {
                        LoadCustomerTransactions(_selectedCustomer.Id);
                    }
                    else
                    {
                        Transactions.Clear();
                        TotalTransactions = 0;
                        TotalSales = 0;
                        TotalPaid = 0;
                        OnPropertyChanged(nameof(HasTransactions));
                        OnPropertyChanged(nameof(ShowEmptyTransactions));
                    }
                }
            }
        }

        public bool HasSelectedCustomer => SelectedCustomer != null;
        public bool HasTransactions => Transactions.Count > 0;
        public bool ShowEmptyTransactions => HasSelectedCustomer && !HasTransactions && !IsLoadingTransactions;
        public bool ShowEmptyCustomers => !HasCustomers && !IsLoading;

        private ObservableCollection<CustomerTransaction> _transactions = new();
        public ObservableCollection<CustomerTransaction> Transactions
        {
            get => _transactions;
            set { _transactions = value; OnPropertyChanged(nameof(Transactions)); OnPropertyChanged(nameof(HasTransactions)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); OnPropertyChanged(nameof(ShowEmptyCustomers)); }
        }

        private bool _isLoadingTransactions;
        public bool IsLoadingTransactions
        {
            get => _isLoadingTransactions;
            set { _isLoadingTransactions = value; OnPropertyChanged(nameof(IsLoadingTransactions)); OnPropertyChanged(nameof(ShowEmptyTransactions)); }
        }

        private int _totalCustomers;
        public int TotalCustomers
        {
            get => _totalCustomers;
            set { _totalCustomers = value; OnPropertyChanged(nameof(TotalCustomers)); }
        }

        private int _totalTransactions;
        public int TotalTransactions
        {
            get => _totalTransactions;
            set { _totalTransactions = value; OnPropertyChanged(nameof(TotalTransactions)); }
        }

        private decimal _totalSales;
        public decimal TotalSales
        {
            get => _totalSales;
            set { _totalSales = value; OnPropertyChanged(nameof(TotalSales)); }
        }

        private decimal _totalPaid;
        public decimal TotalPaid
        {
            get => _totalPaid;
            set { _totalPaid = value; OnPropertyChanged(nameof(TotalPaid)); }
        }

        private decimal _totalDebtors;
        public decimal TotalDebtors
        {
            get => _totalDebtors;
            set { _totalDebtors = value; OnPropertyChanged(nameof(TotalDebtors)); }
        }

        private decimal _totalCreditors;
        public decimal TotalCreditors
        {
            get => _totalCreditors;
            set { _totalCreditors = value; OnPropertyChanged(nameof(TotalCreditors)); }
        }
        #endregion

        #region Commands
        public ICommand RefreshCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand ExportTransactionsCommand { get; }
        public ICommand ExportCustomersCommand { get; }
        #endregion

        public CustomersPageViewModel()
        {
            _ledgerService = App.ServiceProvider.GetRequiredService<ICustomerLedgerService>();

            RefreshCommand = new RelayCommand(_ => LoadCustomers());
            ClearSelectionCommand = new RelayCommand(_ => SelectedCustomer = null);
            DeleteCustomerCommand = new RelayCommand(DeleteCustomer);
            ExportTransactionsCommand = new RelayCommand(_ => ExportTransactions(), _ => HasSelectedCustomer && HasTransactions);
            ExportCustomersCommand = new RelayCommand(_ => ExecuteExportCustomers(), _ => HasCustomers);

            LoadCustomers();
        }

        public void RefreshItems()
        {
            LoadCustomers();
            // Refresh transactions if customer is selected
            if (SelectedCustomer != null)
            {
                LoadCustomerTransactions(SelectedCustomer.Id);
            }
        }

        private void LoadCustomers()
        {
            IsLoading = true;
            try
            {
                using var context = new AppDbContext();

                var query = context.Customers
                    .AsNoTracking()
                    .Where(c => !c.IsArchived);

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var term = $"%{SearchText.Trim()}%";
                    query = query.Where(c =>
                        EF.Functions.Like(c.Name ?? "", term) ||
                        EF.Functions.Like(c.Phone ?? "", term) ||
                        EF.Functions.Like(c.Email ?? "", term) ||
                        EF.Functions.Like(c.City ?? "", term));
                }

                // Order by IsDefault first (Walk-in at top), then by Name
                var customers = query
                    .OrderByDescending(c => c.IsDefault)
                    .ThenBy(c => c.Name)
                    .ToList();

                var balances = _ledgerService.GetCurrentBalances(customers.Select(c => c.Id));

                decimal debtors = 0;
                decimal creditors = 0;

                foreach (var customer in customers)
                {
                    if (balances.TryGetValue(customer.Id, out var balance))
                    {
                        customer.CurrentBalance = balance;
                        if (balance > 0) debtors += balance;
                        else if (balance < 0) creditors += Math.Abs(balance);
                    }
                }

                Customers.Clear();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }
                TotalCustomers = customers.Count;
                TotalDebtors = debtors;
                TotalCreditors = creditors;
                OnPropertyChanged(nameof(HasCustomers));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadCustomerTransactions(int customerId)
        {
            IsLoadingTransactions = true;

            try
            {
                using var context = new AppDbContext();

                var invoices = context.Invoices
                    .AsNoTracking()
                    .Where(i => i.CustomerId == customerId)
                    .OrderByDescending(i => i.Date)
                    .Take(100)
                    .ToList();

                var transactions = invoices.Select(i => new CustomerTransaction
                {
                    Id = i.Id,
                    Number = i.Number,
                    Date = i.Date,
                    Amount = i.TotalPrice,
                    Paid = i.AmountPaid,
                    Status = GetInvoiceStatus(i.Type),
                    Type = "فاتورة بيع",
                    PaymentMethod = i.PaymentMethod ?? "نقدي"
                }).ToList();

                Transactions.Clear();
                foreach (var t in transactions)
                {
                    Transactions.Add(t);
                }
                TotalTransactions = transactions.Count;
                TotalSales = transactions.Sum(t => t.Amount);
                TotalPaid = transactions.Sum(t => t.Paid);
                OnPropertyChanged(nameof(HasTransactions));
                OnPropertyChanged(nameof(ShowEmptyTransactions));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المعاملات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoadingTransactions = false;
            }
        }

        private string GetInvoiceStatus(InvoiceType type)
        {
            return type switch
            {
                InvoiceType.Valid => "مكتملة",
                InvoiceType.Cancelled => "ملغاة",
                InvoiceType.Suspended => "معلقة",
                _ => "غير محدد"
            };
        }

        private void DeleteCustomer(object? parameter)
        {
            if (SelectedCustomer == null) return;

            if (SelectedCustomer.IsDefault)
            {
                MessageBox.Show("لا يمكن حذف العميل الافتراضي.", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"هل تريد حذف العميل '{SelectedCustomer.Name}'؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var context = new AppDbContext();
                    var customer = context.Customers.Find(SelectedCustomer.Id);
                    if (customer != null)
                    {
                        customer.IsArchived = true;
                        context.SaveChanges();
                        Customers.Remove(SelectedCustomer);
                        SelectedCustomer = null;
                        TotalCustomers--;
                        OnPropertyChanged(nameof(HasCustomers));
                        App.NotifyCustomersChanged();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportTransactions()
        {
            if (SelectedCustomer == null || Transactions.Count == 0) return;

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"معاملات_{SelectedCustomer.Name}_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    // UTF-8 BOM for Arabic support
                    sb.AppendLine("رقم الفاتورة,التاريخ,المبلغ,المدفوع,المتبقي,الحالة,طريقة الدفع");

                    foreach (var t in Transactions)
                    {
                        sb.AppendLine($"{t.Number},{t.Date:yyyy-MM-dd},{t.Amount:N2},{t.Paid:N2},{t.Remaining:N2},{t.Status},{t.PaymentMethod}");
                    }

                    // Add summary
                    sb.AppendLine();
                    sb.AppendLine($"إجمالي المبيعات,{TotalSales:N2}");
                    sb.AppendLine($"إجمالي المدفوع,{TotalPaid:N2}");
                    sb.AppendLine($"إجمالي المتبقي,{(TotalSales - TotalPaid):N2}");

                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"تم تصدير {Transactions.Count} معاملة بنجاح", "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportCustomers()
        {
            if (Customers.Count == 0) return;

            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                    FileName = $"العملاء_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var extension = Path.GetExtension(saveDialog.FileName).ToLower();
                    var count = Customers.Count;

                    if (extension == ".xlsx")
                    {
                        ExportToExcel(saveDialog.FileName);
                    }
                    else
                    {
                        ExportToCsv(saveDialog.FileName);
                    }

                    MessageBox.Show($"تم تصدير {count} عميل بنجاح", "تم التصدير", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التصدير: {ex.Message}", "خطأ", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel(string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("العملاء");

            // Set RTL
            worksheet.RightToLeft = true;

            // Headers
            var headers = new[] { "الاسم", "الهاتف", "البريد الإلكتروني", "المدينة", "العنوان", "الرصيد الحالي", "تاريخ الإنشاء" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data
            int row = 2;
            foreach (var customer in Customers)
            {
                worksheet.Cell(row, 1).Value = customer.Name ?? "";
                worksheet.Cell(row, 2).Value = customer.Phone ?? "";
                worksheet.Cell(row, 3).Value = customer.Email ?? "";
                worksheet.Cell(row, 4).Value = customer.City ?? "";
                worksheet.Cell(row, 5).Value = customer.Address ?? "";
                worksheet.Cell(row, 6).Value = customer.CurrentBalance;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 7).Value = customer.CreatedAt?.ToString("yyyy-MM-dd") ?? "";
                
                // Color balance based on value
                if (customer.CurrentBalance > 0)
                {
                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.FromHtml("#EF4444");
                }
                else if (customer.CurrentBalance < 0)
                {
                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.FromHtml("#10B981");
                }
                
                row++;
            }

            // Summary row
            row++;
            worksheet.Cell(row, 1).Value = "الإحصائيات:";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            row++;
            worksheet.Cell(row, 1).Value = "إجمالي العملاء:";
            worksheet.Cell(row, 2).Value = TotalCustomers;
            row++;
            worksheet.Cell(row, 1).Value = "إجمالي المدينون:";
            worksheet.Cell(row, 2).Value = TotalDebtors;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            row++;
            worksheet.Cell(row, 1).Value = "إجمالي الدائنون:";
            worksheet.Cell(row, 2).Value = TotalCreditors;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
            
            workbook.SaveAs(filePath);
        }

        private void ExportToCsv(string filePath)
        {
            var sb = new StringBuilder();
            // UTF-8 BOM for Arabic support
            sb.AppendLine("الاسم,الهاتف,البريد الإلكتروني,المدينة,العنوان,الرصيد الحالي,تاريخ الإنشاء");

            foreach (var customer in Customers)
            {
                var createdDate = customer.CreatedAt?.ToString("yyyy-MM-dd") ?? "";
                sb.AppendLine($"\"{customer.Name ?? ""}\",\"{customer.Phone ?? ""}\",\"{customer.Email ?? ""}\",\"{customer.City ?? ""}\"" +
                            $"\"{customer.Address ?? ""}\",{customer.CurrentBalance:N2},{createdDate}");
            }

            // Summary
            sb.AppendLine();
            sb.AppendLine($"إجمالي العملاء,{TotalCustomers}");
            sb.AppendLine($"إجمالي المدينون,{TotalDebtors:N2}");
            sb.AppendLine($"إجمالي الدائنون,{TotalCreditors:N2}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
