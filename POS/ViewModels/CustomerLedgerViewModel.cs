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

        public CustomerLedgerViewModel()
        {
            _dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>();
            _ledgerService = App.ServiceProvider.GetRequiredService<ICustomerLedgerService>();

            _refreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy && SelectedCustomer != null);
            _clearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync, () => !IsBusy);
            _recordPaymentCommand = new AsyncRelayCommand(RecordPaymentAsync, () => !IsBusy && CanRecordPayment);
            _loadNextPageCommand = new AsyncRelayCommand(LoadNextPageAsync, () => !IsBusy && HasMore);

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
                    ClearLedger();
                    UpdateCommandStates();
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
                }
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

        public bool CanRecordPayment =>
            SelectedCustomer != null
            && !SelectedCustomer.IsDefault
            && PaymentAmount > 0
            && !string.IsNullOrWhiteSpace(SelectedPaymentMethod);

        public ICommand RefreshCommand => _refreshCommand;
        public ICommand ClearFiltersCommand => _clearFiltersCommand;
        public ICommand RecordPaymentCommand => _recordPaymentCommand;
        public ICommand LoadNextPageCommand => _loadNextPageCommand;

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
                    query = query.Where(c => c.Name.Contains(term) || c.Phone.Contains(term));
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
