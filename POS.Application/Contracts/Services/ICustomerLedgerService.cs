using POS.Domain.Models;
using POS.Domain.Models.Payments.PaymentMethods;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Application.Contracts.Services
{
    public interface ICustomerLedgerService
    {
        Customer EnsureDefaultCustomer();
        Task<Customer> EnsureDefaultCustomerAsync(CancellationToken cancellationToken = default);
        Customer? GetDefaultCustomer();
        bool IsDefaultCustomer(Customer? customer);
        bool IsDefaultCustomerId(int? customerId);

        IReadOnlyList<CustomerLedgerEntry> GetStatementEntries(int customerId, DateTime? fromDate, DateTime? toDate);
        Task<IReadOnlyList<CustomerLedgerEntry>> GetStatementEntriesAsync(int customerId, DateTime? fromDate, DateTime? toDate, int skip, int take, CancellationToken cancellationToken = default);
        decimal GetOpeningBalance(int customerId, DateTime? fromDate);
        Task<decimal> GetOpeningBalanceAsync(int customerId, DateTime? fromDate, CancellationToken cancellationToken = default);
        IDictionary<int, decimal> GetCurrentBalances(IEnumerable<int> customerIds);

        void RecordInvoiceEntries(int customerId, string invoiceNumber, DateTime date, decimal totalAmount, decimal amountPaid, string invoicePaymentMethod, string? paymentEntryMethod = null);
        void RecordCustomerPayment(int customerId, decimal amount, string paymentMethod, string? referenceNumber, DateTime date);
        Task RecordCustomerPaymentAsync(int customerId, decimal amount, string paymentMethod, string? referenceNumber, DateTime date, CancellationToken cancellationToken = default);

        int? RecordCashMovement(decimal amount, string? cashName, TransactionType type);
        Task<int?> RecordCashMovementAsync(decimal amount, string? cashName, TransactionType type, CancellationToken cancellationToken = default);
        bool HasCustomerTransactions(int customerId);
    }
}
