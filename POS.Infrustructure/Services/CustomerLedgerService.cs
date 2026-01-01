using POS.Application.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using POS.Domain.Models;
using POS.Domain.Models.Payments.PaymentMethods;
using POS.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace POS.Infrustructure.Services
{
    public class CustomerLedgerService : ICustomerLedgerService
    {
        private readonly AppDbContext _dbContext;
        private const string DefaultCustomerName = "Walk-in";

        public CustomerLedgerService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Customer EnsureDefaultCustomer()
        {
            var customer = _dbContext.Customers
                .AsNoTracking()
                .FirstOrDefault(c => c.IsDefault && !c.IsArchived);
            if (customer != null)
            {
                return customer;
            }

            customer = _dbContext.Customers
                .FirstOrDefault(c => c.Name == DefaultCustomerName && !c.IsArchived);
            if (customer != null)
            {
                customer.IsDefault = true;
                _dbContext.SaveChanges();
                return customer;
            }

            customer = new Customer
            {
                Name = DefaultCustomerName,
                CreatedAt = DateTime.Now,
                PreviousBalance = 0,
                IsDefault = true,
                IsArchived = false
            };

            _dbContext.Customers.Add(customer);
            _dbContext.SaveChanges();
            return customer;
        }

        public async Task<Customer> EnsureDefaultCustomerAsync(CancellationToken cancellationToken = default)
        {
            var customer = await _dbContext.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IsDefault && !c.IsArchived, cancellationToken);
            if (customer != null)
            {
                return customer;
            }

            customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Name == DefaultCustomerName && !c.IsArchived, cancellationToken);
            if (customer != null)
            {
                customer.IsDefault = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return customer;
            }

            customer = new Customer
            {
                Name = DefaultCustomerName,
                CreatedAt = DateTime.Now,
                PreviousBalance = 0,
                IsDefault = true,
                IsArchived = false
            };

            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return customer;
        }

        public Customer? GetDefaultCustomer()
        {
            return _dbContext.Customers.AsNoTracking().FirstOrDefault(c => c.IsDefault && !c.IsArchived)
                ?? _dbContext.Customers.FirstOrDefault(c => c.Name == DefaultCustomerName && !c.IsArchived);
        }

        public bool IsDefaultCustomer(Customer? customer)
        {
            return customer != null
                && !customer.IsArchived
                && (customer.IsDefault || string.Equals(customer.Name, DefaultCustomerName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsDefaultCustomerId(int? customerId)
        {
            if (customerId == null)
            {
                return false;
            }

            var customer = _dbContext.Customers.Find(customerId.Value);
            return IsDefaultCustomer(customer);
        }

        public IReadOnlyList<CustomerLedgerEntry> GetStatementEntries(int customerId, DateTime? fromDate, DateTime? toDate)
        {
            var baseQuery = _dbContext.CustomerLedgerEntries
                .AsNoTracking()
                .Where(e => e.CustomerId == customerId);

            var openingBalance = GetOpeningBalance(customerId, fromDate);

            var query = baseQuery;
            if (fromDate.HasValue)
            {
                query = query.Where(e => e.Date >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(e => e.Date <= toDate.Value);
            }

            var entries = query
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Id)
                .ToList();

            var runningBalance = openingBalance;

            foreach (var entry in entries)
            {
                runningBalance += entry.Credit - entry.Debit;
                entry.RunningBalance = runningBalance;
            }

            return entries;
        }

        public decimal GetOpeningBalance(int customerId, DateTime? fromDate)
        {
            var openingBalance = (decimal)(_dbContext.Customers.Find(customerId)?.PreviousBalance ?? 0d);
            if (fromDate.HasValue)
            {
                var priorNet = _dbContext.CustomerLedgerEntries
                    .Where(e => e.CustomerId == customerId && e.Date < fromDate.Value)
                    .Select(e => (decimal?)(e.Credit - e.Debit))
                    .Sum() ?? 0m;
                openingBalance += priorNet;
            }

            return openingBalance;
        }

        public async Task<IReadOnlyList<CustomerLedgerEntry>> GetStatementEntriesAsync(int customerId, DateTime? fromDate, DateTime? toDate, int skip, int take, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.CustomerLedgerEntries
                .AsNoTracking()
                .Where(e => e.CustomerId == customerId);

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.Date >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(e => e.Date <= toDate.Value);
            }

            return await query
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Id)
                .Skip(skip)
                .Take(take)
                .Select(e => new CustomerLedgerEntry
                {
                    Id = e.Id,
                    CustomerId = e.CustomerId,
                    Date = e.Date,
                    Debit = e.Debit,
                    Credit = e.Credit,
                    Description = e.Description,
                    ReferenceNumber = e.ReferenceNumber,
                    PaymentMethod = e.PaymentMethod
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetOpeningBalanceAsync(int customerId, DateTime? fromDate, CancellationToken cancellationToken = default)
        {
            var openingBalance = await _dbContext.Customers
                .AsNoTracking()
                .Where(c => c.Id == customerId)
                .Select(c => (decimal?)c.PreviousBalance)
                .FirstOrDefaultAsync(cancellationToken) ?? 0m;

            if (fromDate.HasValue)
            {
                var priorNet = await _dbContext.CustomerLedgerEntries
                    .AsNoTracking()
                    .Where(e => e.CustomerId == customerId && e.Date < fromDate.Value)
                    .Select(e => (decimal?)(e.Credit - e.Debit))
                    .SumAsync(cancellationToken) ?? 0m;
                openingBalance += priorNet;
            }

            return openingBalance;
        }

        public void RecordInvoiceEntries(int customerId, string invoiceNumber, DateTime date, decimal totalAmount, decimal amountPaid, string invoicePaymentMethod, string? paymentEntryMethod = null)
        {
            if (IsDefaultCustomerId(customerId))
            {
                return;
            }

            if (totalAmount > 0)
            {
                _dbContext.CustomerLedgerEntries.Add(new CustomerLedgerEntry
                {
                    CustomerId = customerId,
                    Date = date,
                    Debit = totalAmount,
                    Credit = 0,
                    Description = "Invoice",
                    ReferenceNumber = invoiceNumber,
                    PaymentMethod = invoicePaymentMethod
                });
            }

            if (amountPaid > 0)
            {
                var creditPaymentMethod = string.IsNullOrWhiteSpace(paymentEntryMethod)
                    ? invoicePaymentMethod
                    : paymentEntryMethod;

                _dbContext.CustomerLedgerEntries.Add(new CustomerLedgerEntry
                {
                    CustomerId = customerId,
                    Date = date,
                    Debit = 0,
                    Credit = amountPaid,
                    Description = "Payment",
                    ReferenceNumber = invoiceNumber,
                    PaymentMethod = creditPaymentMethod
                });
            }

            _dbContext.SaveChanges();
        }

        public void RecordCustomerPayment(int customerId, decimal amount, string paymentMethod, string? referenceNumber, DateTime date)
        {
            if (amount <= 0 || IsDefaultCustomerId(customerId))
            {
                return;
            }

            _dbContext.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                CustomerId = customerId,
                Date = date,
                Debit = 0,
                Credit = amount,
                Description = "Customer Payment",
                ReferenceNumber = referenceNumber,
                PaymentMethod = paymentMethod
            });

            _dbContext.SaveChanges();
        }

        public async Task RecordCustomerPaymentAsync(int customerId, decimal amount, string paymentMethod, string? referenceNumber, DateTime date, CancellationToken cancellationToken = default)
        {
            if (amount <= 0 || IsDefaultCustomerId(customerId))
            {
                return;
            }

            _dbContext.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                CustomerId = customerId,
                Date = date,
                Debit = 0,
                Credit = amount,
                Description = "Customer Payment",
                ReferenceNumber = referenceNumber,
                PaymentMethod = paymentMethod
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public int? RecordCashMovement(decimal amount, string? cashName, TransactionType type)
        {
            if (amount <= 0)
            {
                return null;
            }

            var cash = new Cash
            {
                CashName = string.IsNullOrWhiteSpace(cashName) ? "POS" : cashName,
                Amount = amount,
                Type = type
            };

            _dbContext.Cashes.Add(cash);
            _dbContext.SaveChanges();
            return cash.Id;
        }

        public async Task<int?> RecordCashMovementAsync(decimal amount, string? cashName, TransactionType type, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                return null;
            }

            var cash = new Cash
            {
                CashName = string.IsNullOrWhiteSpace(cashName) ? "POS" : cashName,
                Amount = amount,
                Type = type
            };

            _dbContext.Cashes.Add(cash);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return cash.Id;
        }

        public bool HasCustomerTransactions(int customerId)
        {
            return _dbContext.Invoices.Any(i => i.CustomerId == customerId)
                || _dbContext.CustomerLedgerEntries.Any(e => e.CustomerId == customerId);
        }
    }
}
