using Microsoft.Extensions.DependencyInjection;
using POS.Application.Contracts.Services;
using POS.Domain.Models;
using POS.Domain.Models.Payments.PaymentMethods;
using POS.Persistence.Context;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace POS.ViewModels
{
    public class CustomerLedgerViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _dbContext;
        private readonly ICustomerLedgerService _ledgerService;

        private ObservableCollection<Customer> _customers;
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set
            {
                _customers = value;
                OnPropertyChanged(nameof(Customers));
            }
        }

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (_selectedCustomer != value)
                {
                    _selectedCustomer = value;
                    OnPropertyChanged(nameof(SelectedCustomer));
                    LoadStatement();
                }
            }
        }

        private ObservableCollection<CustomerLedgerEntry> _ledgerEntries;
        public ObservableCollection<CustomerLedgerEntry> LedgerEntries
        {
            get => _ledgerEntries;
            set
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
                    LoadStatement();
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
                    LoadStatement();
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
                }
            }
        }

        private string _paymentReference;
        public string PaymentReference
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
                if (_selectedPaymentMethod != value)
                {
                    _selectedPaymentMethod = value;
                    OnPropertyChanged(nameof(SelectedPaymentMethod));
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RecordPaymentCommand { get; }

        public CustomerLedgerViewModel()
        {
            _dbContext = new AppDbContext();
            _ledgerService = App.ServiceProvider.GetRequiredService<ICustomerLedgerService>();

            RefreshCommand = new RelayCommand(_ => LoadStatement());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            RecordPaymentCommand = new RelayCommand(_ => RecordPayment());

            PaymentMethods = new ObservableCollection<string>
            {
                "نقدي",
                "بطاقة ائتمان",
                "محفظة الكترونية"
            };
            SelectedPaymentMethod = PaymentMethods.FirstOrDefault();

            LoadCustomers();
            LoadStatement();
        }

        private void LoadCustomers()
        {
            _ledgerService.EnsureDefaultCustomer();

            var customers = _dbContext.Customers
                .Where(c => !c.IsArchived)
                .ToList();

            Customers = new ObservableCollection<Customer>(customers);
            SelectedCustomer = Customers.FirstOrDefault(c => !c.IsDefault)
                ?? Customers.FirstOrDefault();
        }

        private void LoadStatement()
        {
            if (SelectedCustomer == null)
            {
                LedgerEntries = new ObservableCollection<CustomerLedgerEntry>();
                CurrentBalance = 0;
                return;
            }

            var entries = _ledgerService.GetStatementEntries(SelectedCustomer.Id, StartDate, EndDate);
            LedgerEntries = new ObservableCollection<CustomerLedgerEntry>(entries);
            var openingBalance = _ledgerService.GetOpeningBalance(SelectedCustomer.Id, StartDate);
            CurrentBalance = entries.LastOrDefault()?.RunningBalance
                ?? openingBalance;
        }

        private void ClearFilters()
        {
            StartDate = null;
            EndDate = null;
        }

        private void RecordPayment()
        {
            if (SelectedCustomer == null)
            {
                MessageBox.Show("اختر العميل أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_ledgerService.IsDefaultCustomer(SelectedCustomer))
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
            _ledgerService.RecordCustomerPayment(SelectedCustomer.Id, amount, SelectedPaymentMethod, PaymentReference, DateTime.Now);

            if (SelectedPaymentMethod == "نقدي")
            {
                _ledgerService.RecordCashMovement(amount, "Customer Payment", TransactionType.Income);
            }

            PaymentAmount = 0;
            PaymentReference = string.Empty;
            LoadStatement();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
