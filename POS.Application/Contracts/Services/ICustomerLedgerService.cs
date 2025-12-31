using POS.Domain.Models;
using POS.Domain.Models.Payments.PaymentMethods;
using System.Collections.Generic;

namespace POS.Application.Contracts.Services
{
    public interface ICustomerLedgerService
    {
        Customer EnsureDefaultCustomer();
        Customer? GetDefaultCustomer();
        bool IsDefaultCustomer(Customer? customer);
        bool IsDefaultCustomerId(int? customerId);

        IReadOnlyList<CustomerLedgerEntry> GetStatementEntries(int customerId, DateTime? fromDate, DateTime? toDate);
        decimal GetOpeningBalance(int customerId, DateTime? fromDate);

        void RecordInvoiceEntries(int customerId, string invoiceNumber, DateTime date, decimal totalAmount, decimal amountPaid, string invoicePaymentMethod, string? paymentEntryMethod = null);
        void RecordCustomerPayment(int customerId, decimal amount, string paymentMethod, string? referenceNumber, DateTime date);

        int? RecordCashMovement(decimal amount, string? cashName, TransactionType type);
        bool HasCustomerTransactions(int customerId);
    }
}
