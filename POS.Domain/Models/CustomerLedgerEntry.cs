using System.ComponentModel.DataAnnotations.Schema;

namespace POS.Domain.Models
{
    public class CustomerLedgerEntry : BaseEntity
    {
        public int Id { get; set; }

        private int _customerId;
        public int CustomerId
        {
            get => _customerId;
            set
            {
                if (_customerId != value)
                {
                    _customerId = value;
                    NotifyPropertyChanged(nameof(CustomerId));
                }
            }
        }

        private Customer? _customer;
        public Customer? Customer
        {
            get => _customer;
            set
            {
                if (_customer != value)
                {
                    _customer = value;
                    NotifyPropertyChanged(nameof(Customer));
                }
            }
        }

        private DateTime _date = DateTime.Now;
        public DateTime Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    NotifyPropertyChanged(nameof(Date));
                }
            }
        }

        private decimal _debit;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Debit
        {
            get => _debit;
            set
            {
                if (_debit != value)
                {
                    _debit = value;
                    NotifyPropertyChanged(nameof(Debit));
                }
            }
        }

        private decimal _credit;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Credit
        {
            get => _credit;
            set
            {
                if (_credit != value)
                {
                    _credit = value;
                    NotifyPropertyChanged(nameof(Credit));
                }
            }
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    NotifyPropertyChanged(nameof(Description));
                }
            }
        }

        private string? _referenceNumber;
        public string? ReferenceNumber
        {
            get => _referenceNumber;
            set
            {
                if (_referenceNumber != value)
                {
                    _referenceNumber = value;
                    NotifyPropertyChanged(nameof(ReferenceNumber));
                }
            }
        }

        private string? _paymentMethod;
        public string? PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (_paymentMethod != value)
                {
                    _paymentMethod = value;
                    NotifyPropertyChanged(nameof(PaymentMethod));
                }
            }
        }

        [NotMapped]
        public decimal RunningBalance { get; set; }
    }
}
