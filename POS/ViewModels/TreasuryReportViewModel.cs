using POS.Domain.Models.Payments.PaymentMethods;
using POS.Persistence.Context;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace POS.ViewModels
{
    public class TreasuryReportViewModel : ViewModelBase
    {
        private readonly AppDbContext _dbContext;

        private ObservableCollection<Cash> _cashEntries;
        public ObservableCollection<Cash> CashEntries
        {
            get => _cashEntries;
            private set
            {
                _cashEntries = value;
                OnPropertyChanged(nameof(CashEntries));
            }
        }

        private ObservableCollection<string> _transactionTypes;
        public ObservableCollection<string> TransactionTypes
        {
            get => _transactionTypes;
            private set
            {
                _transactionTypes = value;
                OnPropertyChanged(nameof(TransactionTypes));
            }
        }

        private string _selectedTransactionType;
        public string SelectedTransactionType
        {
            get => _selectedTransactionType;
            set
            {
                if (_selectedTransactionType != value)
                {
                    _selectedTransactionType = value;
                    OnPropertyChanged(nameof(SelectedTransactionType));
                }
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

        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            private set
            {
                if (_totalIncome != value)
                {
                    _totalIncome = value;
                    OnPropertyChanged(nameof(TotalIncome));
                }
            }
        }

        private decimal _totalOutcome;
        public decimal TotalOutcome
        {
            get => _totalOutcome;
            private set
            {
                if (_totalOutcome != value)
                {
                    _totalOutcome = value;
                    OnPropertyChanged(nameof(TotalOutcome));
                }
            }
        }

        private decimal _netBalance;
        public decimal NetBalance
        {
            get => _netBalance;
            private set
            {
                if (_netBalance != value)
                {
                    _netBalance = value;
                    OnPropertyChanged(nameof(NetBalance));
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public TreasuryReportViewModel()
        {
            _dbContext = new AppDbContext();

            TransactionTypes = new ObservableCollection<string>
            {
                "الكل",
                "الوارد",
                "الصادر"
            };
            SelectedTransactionType = "الكل";

            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;

            RefreshCommand = new ViewModelCommand(_ => LoadEntries());
            ClearFiltersCommand = new ViewModelCommand(_ => ClearFilters());

            LoadEntries();
        }

        private void LoadEntries()
        {
            var query = _dbContext.Cashes.AsQueryable();

            if (StartDate.HasValue)
            {
                var fromDate = StartDate.Value.Date;
                query = query.Where(c => c.CreatedDate >= fromDate);
            }

            if (EndDate.HasValue)
            {
                var toDate = EndDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(c => c.CreatedDate <= toDate);
            }

            if (SelectedTransactionType == "الوارد")
            {
                query = query.Where(c => c.Type == TransactionType.Income);
            }
            else if (SelectedTransactionType == "الصادر")
            {
                query = query.Where(c => c.Type == TransactionType.Outcome);
            }

            var entries = query
                .OrderByDescending(c => c.CreatedDate)
                .ToList();

            CashEntries = new ObservableCollection<Cash>(entries);

            TotalIncome = entries.Where(e => e.Type == TransactionType.Income).Sum(e => e.Amount);
            TotalOutcome = entries.Where(e => e.Type == TransactionType.Outcome).Sum(e => e.Amount);
            NetBalance = TotalIncome - TotalOutcome;
        }

        private void ClearFilters()
        {
            SelectedTransactionType = "الكل";
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
            LoadEntries();
        }
    }
}
