using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Contracts.Services;
using POS.Domain.Models;
using POS.Domain.Models.Payments.PaymentMethods;
using POS.Persistence.Context;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace POS.ViewModels
{
    public class CustomerLedgerViewModel : INotifyPropertyChanged, IDisposable
    {
        private const int DefaultCustomerPageSize = 100;
        private const int DefaultLedgerPageSize = 200;

        private readonly AppDbContext _dbContext;
        private readonly ICustomerLedgerService _ledgerService;
        private readonly Debouncer _customerSearchDebouncer = new Debouncer();
        private readonly SemaphoreSlim _ledgerGate = new SemaphoreSlim(1, 1);

        private CancellationTokenSource? _ledgerLoadCts;
        private bool _isInitialized;
        private decimal _openingBalance;
        private int _nextPage = 1;

        private readonly AsyncRelayCommand _refreshCommand;
        private readonly AsyncRelayCommand _clearFiltersCommand;
        private readonly AsyncRelayCommand _recordPaymentCommand;
        private readonly AsyncRelayCommand _loadNextPageCommand;
        private readonly AsyncRelayCommand _exportCommand;

        public CustomerLedgerViewModel()
        {
            _dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>();
            _ledgerService = App.ServiceProvider.GetRequiredService<ICustomerLedgerService>();

            _refreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy && SelectedCustomer != null);
            _clearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync, () => !IsBusy);
            _recordPaymentCommand = new AsyncRelayCommand(RecordPaymentAsync, () => !IsBusy && CanRecordPayment);
            _loadNextPageCommand = new AsyncRelayCommand(LoadNextPageAsync, () => !IsBusy && HasMore);
            _exportCommand = new AsyncRelayCommand(ExecuteExportAsync, () => !IsBusy && HasLedgerEntries);

            PaymentMethods = new ObservableCollection<string>
            {
                "نقدي",
                "بطاقة ائتمان",
                "تحويل بنكي"
            };
            SelectedPaymentMethod = PaymentMethods.FirstOrDefault();

            Customers = new ObservableCollection<CustomerLookup>();
            LedgerEntries = new ObservableCollection<CustomerLedgerEntry>();
        }

        public ObservableCollection<CustomerLookup> Customers { get; private set; }

        private CustomerLookup? _selectedCustomer;
        public CustomerLookup? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (_selectedCustomer != value)
                {
                    _selectedCustomer = value;
                    OnPropertyChanged(nameof(SelectedCustomer));
                    OnPropertyChanged(nameof(ShowSelectCustomer));
                    OnPropertyChanged(nameof(ShowEmptyState));
                    ClearLedger();
                    UpdateCommandStates();
                    
                    // Auto-load ledger when customer is selected
                    if (_selectedCustomer != null && _isInitialized)
                    {
                        _ = LoadLedgerPageAsync(reset: true);
                    }
                }
            }
        }

        private ObservableCollection<CustomerLedgerEntry> _ledgerEntries;
        public ObservableCollection<CustomerLedgerEntry> LedgerEntries
        {
            get => _ledgerEntries;
            private set
            {
                _ledgerEntries = value;
                OnPropertyChanged(nameof(LedgerEntries));
                OnPropertyChanged(nameof(HasLedgerEntries));
                OnPropertyChanged(nameof(ShowEmptyState));
                OnPropertyChanged(nameof(TotalPayments));
                OnPropertyChanged(nameof(TotalPurchases));
            }
        }

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged(nameof(StartDate));
                }
            }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged(nameof(EndDate));
                }
            }
        }

        private decimal _currentBalance;
        public decimal CurrentBalance
        {
            get => _currentBalance;
            set
            {
                if (_currentBalance != value)
                {
                    _currentBalance = value;
                    OnPropertyChanged(nameof(CurrentBalance));
                    OnPropertyChanged(nameof(Balance));
                }
            }
        }

        // Computed properties for statistics
        public string TotalPayments => (LedgerEntries?.Sum(e => e.Credit) ?? 0).ToString("N2") + " جنيه";
        public string TotalPurchases => (LedgerEntries?.Sum(e => e.Debit) ?? 0).ToString("N2") + " جنيه";
        public string Balance
        {
            get
            {
                // Balance = Debit - Credit (what customer owes us - what they paid)
                // Positive = customer owes us (له علينا)
                // Negative = we owe customer (لنا عليه)
                if (CurrentBalance > 0)
                    return $"{CurrentBalance:N2} جنيه (له علينا)";
                else if (CurrentBalance < 0)
                    return $"{Math.Abs(CurrentBalance):N2} جنيه (لنا عليه)";
                else
                    return "0.00 جنيه (متساوي)";
            }
        }


        private double _paymentAmount;
        public double PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                if (Math.Abs(_paymentAmount - value) > 0.001)
                {
                    _paymentAmount = value;
                    OnPropertyChanged(nameof(PaymentAmount));
                    UpdateCommandStates();
                }
            }
        }

        private string? _paymentReference;
        public string? PaymentReference
        {
            get => _paymentReference;
            set
            {
                if (_paymentReference != value)
                {
                    _paymentReference = value;
                    OnPropertyChanged(nameof(PaymentReference));
                }
            }
        }

        public ObservableCollection<string> PaymentMethods { get; }

        private string? _selectedPaymentMethod;
        public string? SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set
            {
                if (_selectedPaymentMethod != value)
                {
                    _selectedPaymentMethod = value;
                    OnPropertyChanged(nameof(SelectedPaymentMethod));
                    UpdateCommandStates();
                }
            }
        }

        private string? _customerSearchText;
        public string? CustomerSearchText
        {
            get => _customerSearchText;
            set
            {
                if (_customerSearchText != value)
                {
                    _customerSearchText = value;
                    OnPropertyChanged(nameof(CustomerSearchText));
                    DebounceCustomerSearch();
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
                    OnPropertyChanged(nameof(IsNotBusy));
                    OnPropertyChanged(nameof(ShowEmptyState));
                    OnPropertyChanged(nameof(ShowSelectCustomer));
                    UpdateCommandStates();
                }
            }
        }

        public bool IsNotBusy => !IsBusy;

        private bool _isCustomerLoading;
        public bool IsCustomerLoading
        {
            get => _isCustomerLoading;
            private set
            {
                if (_isCustomerLoading != value)
                {
                    _isCustomerLoading = value;
                    OnPropertyChanged(nameof(IsCustomerLoading));
                }
            }
        }

        private string? _busyMessage;
        public string? BusyMessage
        {
            get => _busyMessage;
            private set
            {
                if (_busyMessage != value)
                {
                    _busyMessage = value;
                    OnPropertyChanged(nameof(BusyMessage));
                }
            }
        }

        private bool _hasMore;
        public bool HasMore
        {
            get => _hasMore;
            private set
            {
                if (_hasMore != value)
                {
                    _hasMore = value;
                    OnPropertyChanged(nameof(HasMore));
                    UpdateCommandStates();
                }
            }
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged(nameof(ErrorMessage));
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool HasCustomers => Customers?.Count > 0;
        public bool HasLedgerEntries => LedgerEntries?.Count > 0;
        public bool ShowEmptyState => !IsBusy && SelectedCustomer != null && !HasLedgerEntries;
        public bool ShowSelectCustomer => !IsBusy && SelectedCustomer == null && HasCustomers;

        public bool CanRecordPayment =>
            SelectedCustomer != null
            && !SelectedCustomer.IsDefault
            && PaymentAmount > 0
            && !string.IsNullOrWhiteSpace(SelectedPaymentMethod);

        public ICommand RefreshCommand => _refreshCommand;
        public ICommand ClearFiltersCommand => _clearFiltersCommand;
        public ICommand RecordPaymentCommand => _recordPaymentCommand;
        public ICommand LoadNextPageCommand => _loadNextPageCommand;
        public ICommand ExportCommand => _exportCommand;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            await LoadCustomersAsync(CustomerSearchText, CancellationToken.None, preserveSelection: false);
        }

        public void CancelPending()
        {
            _ledgerLoadCts?.Cancel();
            _customerSearchDebouncer.Cancel();
        }

        private void DebounceCustomerSearch()
        {
            if (!_isInitialized)
            {
                return;
            }

            _ = _customerSearchDebouncer.DebounceAsync(
                token => LoadCustomersAsync(CustomerSearchText, token, preserveSelection: true),
                300);
        }

        private async Task LoadCustomersAsync(string? searchText, CancellationToken cancellationToken, bool preserveSelection)
        {
            IsCustomerLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                await _ledgerService.EnsureDefaultCustomerAsync(cancellationToken);

                var query = _dbContext.Customers
                    .AsNoTracking()
                    .Where(c => !c.IsArchived);

                var term = searchText?.Trim();
                if (!string.IsNullOrWhiteSpace(term))
                {
                    var pattern = $"%{term}%";
                    query = query.Where(c => 
                        EF.Functions.Like(c.Name ?? "", pattern) || 
                        EF.Functions.Like(c.Phone ?? "", pattern));
                }

                var customers = await query
                    .OrderByDescending(c => c.IsDefault)
                    .ThenBy(c => c.Name)
                    .Take(DefaultCustomerPageSize)
                    .Select(c => new CustomerLookup
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Phone = c.Phone,
                        IsDefault = c.IsDefault
                    })
                    .ToListAsync(cancellationToken);

                Customers = new ObservableCollection<CustomerLookup>(customers);
                OnPropertyChanged(nameof(Customers));
                OnPropertyChanged(nameof(HasCustomers));
                OnPropertyChanged(nameof(ShowSelectCustomer));

                if (preserveSelection && SelectedCustomer != null)
                {
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == SelectedCustomer.Id)
                        ?? Customers.FirstOrDefault();
                }
                else
                {
                    SelectedCustomer = Customers.FirstOrDefault();
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellations.
            }
            catch
            {
                ErrorMessage = "حدث خطأ أثناء تحميل العملاء.";
            }
            finally
            {
                IsCustomerLoading = false;
            }
        }

        private async Task RefreshAsync()
        {
            ErrorMessage = string.Empty;

            if (SelectedCustomer == null)
            {
                MessageBox.Show("اختر العميل أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await LoadLedgerPageAsync(reset: true);
        }

        private async Task LoadNextPageAsync()
        {
            if (SelectedCustomer == null || !HasMore)
            {
                return;
            }

            await LoadLedgerPageAsync(reset: false);
        }

        private async Task LoadLedgerPageAsync(bool reset)
        {
            if (SelectedCustomer == null)
            {
                return;
            }

            _ledgerLoadCts?.Cancel();
            _ledgerLoadCts?.Dispose();
            _ledgerLoadCts = new CancellationTokenSource();
            var token = _ledgerLoadCts.Token;

            var lockTaken = false;
            try
            {
                IsBusy = true;
                BusyMessage = "جار تحميل كشف الحساب...";

                await _ledgerGate.WaitAsync(token);
                lockTaken = true;

                var customerId = SelectedCustomer.Id;
                var startDate = StartDate;
                var endDate = EndDate;

                if (reset)
                {
                    _openingBalance = await _ledgerService.GetOpeningBalanceAsync(customerId, startDate, token);
                    _nextPage = 1;
                    LedgerEntries = new ObservableCollection<CustomerLedgerEntry>();
                    CurrentBalance = _openingBalance;
                }

                var skip = (_nextPage - 1) * DefaultLedgerPageSize;
                var entries = await _ledgerService.GetStatementEntriesAsync(
                    customerId,
                    startDate,
                    endDate,
                    skip,
                    DefaultLedgerPageSize + 1,
                    token);

                if (SelectedCustomer == null || SelectedCustomer.Id != customerId)
                {
                    return;
                }

                var pageEntries = entries.Take(DefaultLedgerPageSize).ToList();
                HasMore = entries.Count > DefaultLedgerPageSize;

                var runningBalance = LedgerEntries.LastOrDefault()?.RunningBalance ?? _openingBalance;
                foreach (var entry in pageEntries)
                {
                    runningBalance += entry.Credit - entry.Debit;
                    entry.RunningBalance = runningBalance;
                }

                if (reset)
                {
                    LedgerEntries = new ObservableCollection<CustomerLedgerEntry>(pageEntries);
                }
                else
                {
                    var combined = LedgerEntries.Concat(pageEntries).ToList();
                    LedgerEntries = new ObservableCollection<CustomerLedgerEntry>(combined);
                }

                CurrentBalance = LedgerEntries.LastOrDefault()?.RunningBalance ?? _openingBalance;

                if (HasMore)
                {
                    _nextPage++;
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellations.
            }
            catch
            {
                ErrorMessage = "حدث خطأ أثناء تحميل كشف الحساب.";
            }
            finally
            {
                if (lockTaken)
                {
                    _ledgerGate.Release();
                }

                BusyMessage = string.Empty;
                IsBusy = false;
            }
        }

        private async Task ClearFiltersAsync()
        {
            StartDate = null;
            EndDate = null;
            ClearLedger();
            await Task.CompletedTask;
        }

        private async Task RecordPaymentAsync()
        {
            if (SelectedCustomer == null)
            {
                MessageBox.Show("اختر العميل أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedCustomer.IsDefault)
            {
                MessageBox.Show("لا يمكن تسجيل تحصيل للعميل الافتراضي.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PaymentAmount <= 0)
            {
                MessageBox.Show("أدخل مبلغ التحصيل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedPaymentMethod))
            {
                MessageBox.Show("اختر طريقة الدفع.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var amount = (decimal)PaymentAmount;

            try
            {
                IsBusy = true;
                BusyMessage = "جار تسجيل التحصيل...";

                await _ledgerService.RecordCustomerPaymentAsync(
                    SelectedCustomer.Id,
                    amount,
                    SelectedPaymentMethod,
                    PaymentReference,
                    DateTime.Now,
                    CancellationToken.None);

                if (SelectedPaymentMethod == "نقدي")
                {
                    await _ledgerService.RecordCashMovementAsync(amount, "Customer Payment", TransactionType.Income, CancellationToken.None);
                }

                PaymentAmount = 0;
                PaymentReference = string.Empty;

                await LoadLedgerPageAsync(reset: true);
            }
            catch
            {
                ErrorMessage = "حدث خطأ أثناء تسجيل التحصيل.";
            }
            finally
            {
                BusyMessage = string.Empty;
                IsBusy = false;
            }
        }

        private async Task ExecuteExportAsync()
        {
            if (!HasLedgerEntries || SelectedCustomer == null)
            {
                MessageBox.Show("لا توجد معاملات للتصدير.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx|CSV Files|*.csv",
                FileName = $"كشف_حساب_{SelectedCustomer.Name}_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                IsBusy = true;
                BusyMessage = "جار تصدير البيانات...";

                var extension = System.IO.Path.GetExtension(dialog.FileName).ToLower();
                
                if (extension == ".xlsx")
                    await ExportToExcelAsync(dialog.FileName);
                else if (extension == ".csv")
                    await ExportToCsvAsync(dialog.FileName);

                MessageBox.Show("تم التصدير بنجاح!", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BusyMessage = string.Empty;
                IsBusy = false;
            }
        }

        private async Task ExportToExcelAsync(string filePath)
        {
            await Task.Run(() =>
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("كشف الحساب");

                // RTL
                worksheet.RightToLeft = true;

                // Header
                worksheet.Cell(1, 1).Value = $"كشف حساب: {SelectedCustomer?.Name}";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Range(1, 1, 1, 6).Merge();

                worksheet.Cell(2, 1).Value = $"التاريخ: {DateTime.Now:dd/MM/yyyy}";
                if (StartDate.HasValue || EndDate.HasValue)
                {
                    var period = StartDate.HasValue && EndDate.HasValue
                        ? $"من {StartDate:dd/MM/yyyy} إلى {EndDate:dd/MM/yyyy}"
                        : StartDate.HasValue
                            ? $"من {StartDate:dd/MM/yyyy}"
                            : $"إلى {EndDate:dd/MM/yyyy}";
                    worksheet.Cell(3, 1).Value = $"الفترة: {period}";
                }

                // Column headers
                var headerRow = 5;
                worksheet.Cell(headerRow, 1).Value = "#";
                worksheet.Cell(headerRow, 2).Value = "التاريخ";
                worksheet.Cell(headerRow, 3).Value = "الوصف";
                worksheet.Cell(headerRow, 4).Value = "مدين";
                worksheet.Cell(headerRow, 5).Value = "دائن";
                worksheet.Cell(headerRow, 6).Value = "الرصيد";

                var headerRange = worksheet.Range(headerRow, 1, headerRow, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#3B82F6");
                headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                // Data
                var row = headerRow + 1;
                var index = 1;
                foreach (var entry in LedgerEntries)
                {
                    worksheet.Cell(row, 1).Value = index++;
                    worksheet.Cell(row, 2).Value = entry.Date.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 3).Value = entry.Description ?? "";
                    worksheet.Cell(row, 4).Value = entry.Debit;
                    worksheet.Cell(row, 5).Value = entry.Credit;
                    worksheet.Cell(row, 6).Value = entry.RunningBalance;

                    // Color for balance
                    if (entry.RunningBalance > 0)
                        worksheet.Cell(row, 6).Style.Font.FontColor = ClosedXML.Excel.XLColor.Red;
                    else if (entry.RunningBalance < 0)
                        worksheet.Cell(row, 6).Style.Font.FontColor = ClosedXML.Excel.XLColor.Green;

                    row++;
                }

                // Summary
                row++;
                worksheet.Cell(row, 1).Value = "الإجماليات";
                worksheet.Cell(row, 1).Style.Font.Bold = true;
                worksheet.Cell(row, 4).Value = LedgerEntries.Sum(e => e.Debit);
                worksheet.Cell(row, 4).Style.Font.Bold = true;
                worksheet.Cell(row, 5).Value = LedgerEntries.Sum(e => e.Credit);
                worksheet.Cell(row, 5).Style.Font.Bold = true;
                worksheet.Cell(row, 6).Value = CurrentBalance;
                worksheet.Cell(row, 6).Style.Font.Bold = true;

                if (CurrentBalance > 0)
                    worksheet.Cell(row, 6).Style.Font.FontColor = ClosedXML.Excel.XLColor.Red;
                else if (CurrentBalance < 0)
                    worksheet.Cell(row, 6).Style.Font.FontColor = ClosedXML.Excel.XLColor.Green;

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
            });
        }

        private async Task ExportToCsvAsync(string filePath)
        {
            await Task.Run(() =>
            {
                using var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8);
                
                // UTF-8 BOM for Excel
                writer.WriteLine($"# كشف حساب: {SelectedCustomer?.Name}");
                writer.WriteLine($"# التاريخ: {DateTime.Now:dd/MM/yyyy}");
                
                if (StartDate.HasValue || EndDate.HasValue)
                {
                    var period = StartDate.HasValue && EndDate.HasValue
                        ? $"من {StartDate:dd/MM/yyyy} إلى {EndDate:dd/MM/yyyy}"
                        : StartDate.HasValue
                            ? $"من {StartDate:dd/MM/yyyy}"
                            : $"إلى {EndDate:dd/MM/yyyy}";
                    writer.WriteLine($"# الفترة: {period}");
                }
                writer.WriteLine();

                // Header
                writer.WriteLine("#,التاريخ,الوصف,مدين,دائن,الرصيد");

                // Data
                var index = 1;
                foreach (var entry in LedgerEntries)
                {
                    var description = entry.Description?.Replace(",", "،") ?? "";
                    writer.WriteLine($"{index},{entry.Date:dd/MM/yyyy},{description},{entry.Debit},{entry.Credit},{entry.RunningBalance}");
                    index++;
                }

                // Summary
                writer.WriteLine();
                writer.WriteLine($"الإجماليات,,,{LedgerEntries.Sum(e => e.Debit)},{LedgerEntries.Sum(e => e.Credit)},{CurrentBalance}");
            });
        }

        private void ClearLedger()

        {
            LedgerEntries = new ObservableCollection<CustomerLedgerEntry>();
            CurrentBalance = 0;
            HasMore = false;
            _openingBalance = 0;
            _nextPage = 1;
        }

        private void UpdateCommandStates()
        {
            _refreshCommand.RaiseCanExecuteChanged();
            _clearFiltersCommand.RaiseCanExecuteChanged();
            _recordPaymentCommand.RaiseCanExecuteChanged();
            _loadNextPageCommand.RaiseCanExecuteChanged();
            _exportCommand.RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            _ledgerLoadCts?.Cancel();
            _ledgerLoadCts?.Dispose();
            _customerSearchDebouncer.Dispose();
            _dbContext.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CustomerLookup
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public bool IsDefault { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Phone) ? (Name ?? string.Empty) : $"{Name} - {Phone}";
    }
}
