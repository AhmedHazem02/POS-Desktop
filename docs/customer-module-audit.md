# Customer Module Audit Report

## A) Project Map

### MVVM Framework
- **Pattern**: Custom MVVM implementation using `INotifyPropertyChanged`
- **Commands**: `RelayCommand` and `ViewModelCommand` classes
- **No MVVM framework**: Pure WPF with manual property change notifications

### DataAccess
- **ORM**: Entity Framework Core
- **Database**: SQL Server (via `AppDbContext`)
- **Context Location**: `POS.Persistence.Context.AppDbContext`
- **Pattern**: Direct DbContext usage in ViewModels (not Repository pattern)

### Key Folders
```
POS/
├── Views/                      # Main windows (LoginView, HomeWindow)
├── ViewModels/                 # All ViewModels
│   └── Base/                   # BaseProductsViewModel (shared POS logic)
├── CustomControl/              # UserControls for each feature
├── Dialogs/                    # Dialog windows
├── Validations/                # Converters and behaviors

POS.Domain/
├── Models/                     # All entities
│   ├── Customer.cs             # Customer entity
│   ├── CustomerLedgerEntry.cs  # Ledger entries
│   ├── Invoice.cs              # Sales invoice
│   └── Payments/               # Payment-related models
│       └── PaymentMethods/     # Cash, etc.

POS.Application/
└── Contracts/Services/         # Service interfaces
    └── ICustomerLedgerService.cs

POS.Infrustructure/
└── Services/                   # Service implementations
    └── CustomerLedgerService.cs

POS.Persistence/
└── Context/
    └── AppDbContext.cs         # EF Core DbContext
```

---

## B) Existing Components (with exact paths)

### Customer Entity/Model
- **Entity**: `POS.Domain.Models.Customer`
- **Path**: `d:\POS\POS.Domain\Models\Customer.cs`
- **Properties**:
  - `Id`, `Name`, `Phone`, `Email`, `Address`, `Notes`
  - `IsDefault` (bool) - marks default/walk-in customer
  - `IsArchived` (bool) - soft delete mechanism
  - Navigation: `Invoices`, `LedgerEntries`

### CustomerLedgerEntry Entity
- **Entity**: `POS.Domain.Models.CustomerLedgerEntry`
- **Path**: `d:\POS\POS.Domain\Models\CustomerLedgerEntry.cs`
- **Properties**:
  - `Id`, `CustomerId`, `Date`, `ReferenceType`, `ReferenceNumber`
  - `Description`, `Debit`, `Credit`, `RunningBalance`
  - `PaymentMethod`
  - Navigation: `Customer`

### Customer List/Details UI
- **Add Customer**: `POS.CustomControl.Customer_Add_UserControl`
- **Customer List**: `POS.CustomControl.Customers_UserControl`
- **ViewModel**: `POS.ViewModels.AddOrEditPersonViewModel`

### POS/Sale/Invoice Workflow
- **POS View**: `POS.CustomControl.POS_UserControl`
- **POS ViewModel**: `POS.ViewModels.POSViewModel` (extends `BaseProductsViewModel`)
- **Base ViewModel**: `POS.ViewModels.Base.BaseProductsViewModel`
  - Contains: `SelectedCustomer`, `Customers`, `CustomersView`, `CustomerSearchText`
  - Loads customers in constructor via `LoadCustomers()`
  - Uses `ICustomerLedgerService.EnsureDefaultCustomer()` to get/create default customer
- **Invoice Entity**: `POS.Domain.Models.Invoice`
  - Has `CustomerId` and `Customer` navigation property
- **Sales History**: `POS.CustomControl.SalesHistory_UserControl`
- **Sales History ViewModel**: `POS.ViewModels.SalesHistoryViewModel`

### Payment/Cash Workflow
- **Cash Entity**: `POS.Domain.Models.Payments.PaymentMethods.Cash`
- **Transaction Types**: `TransactionType` enum (Income, Outcome)
- **Currency Types**: `CurrencyType` enum (EGP, USD, etc.)
- **Invoice has**: `AmountPaid`, `ChangeAmount`, `PaymentMethod`, `BillBreakdown`
- **InvoicePayment**: Links Invoice to Cash payments

### Ledger/Statement/AR
- **Service Interface**: `POS.Application.Contracts.Services.ICustomerLedgerService`
- **Service Implementation**: `POS.Infrustructure.Services.CustomerLedgerService`
- **Key Methods**:
  - `EnsureDefaultCustomer()` / `EnsureDefaultCustomerAsync()`
  - `GetDefaultCustomer()`, `IsDefaultCustomer()`, `IsDefaultCustomerId()`
  - `GetStatementEntries()` - returns entries with RunningBalance
  - `GetOpeningBalance()` - calculates balance before a date
  - `GetCurrentBalances()` - batch balance lookup
  - `RecordInvoiceEntries()` - records sale + payment ledger entries
  - `RecordCustomerPayment()` - records customer payment
  - `RecordCashMovement()` - records cash drawer movement
  - `HasCustomerTransactions()` - prevents deletion of customer with transactions

### Soft Delete Mechanism
- **Customer.IsArchived**: Boolean flag for soft delete
- **Query filtering**: `_dbContext.Customers.Where(c => !c.IsArchived)`
- **Deletion protection**: `HasCustomerTransactions()` checks before archive

### DI Registration
- **Location**: `POS.Infrustructure.InfrustructureDependencies`
- **Services registered**:
  - `IExcelService` -> `ExcelService`
  - `ICustomerLedgerService` -> `CustomerLedgerService`

---

## C) Discovered Naming

| Component | Discovered Name |
|-----------|-----------------|
| Entity name for customer | `Customer` |
| Default/Walk-in customer name | `"عميل افتراضي"` (created by `EnsureDefaultCustomer`) |
| Customer.IsDefault flag | `IsDefault = true` marks default customer |
| Service for sales | N/A (logic in ViewModels) |
| Service for payments/cash | `ICustomerLedgerService.RecordCashMovement()` |
| Service for ledger | `ICustomerLedgerService` |
| Payment methods (Invoice) | String: `"نقدي"`, `"آجل"`, `"بطاقة ائتمان"`, `"محفظة الكترونية"` |
| Transaction types | `TransactionType.Income`, `TransactionType.Outcome` |
| Soft delete field | `Customer.IsArchived` |

---

## D) Gaps

### Existing and Working:
1. Customer entity with `IsDefault` and `IsArchived`
2. CustomerLedgerEntry with Debit/Credit/RunningBalance
3. ICustomerLedgerService fully implemented:
   - Default customer creation
   - Ledger entry recording
   - Cash movement recording
   - Customer payment recording
   - Statement with running balance
   - Deletion protection
4. Customer selection in POS (BaseProductsViewModel)
5. Customer list/add UI

### Minor Gaps to Address:
1. **POS Save Integration**: Need to verify `POSViewModel.CompletePayment` calls ledger service
2. **Customer Statement UI**: May need dedicated view for statement display
3. **Customer Payment UI**: Need dedicated view for recording customer payments (if not exists)
4. **Due Amount Handling**: Verify due amount creates ledger debit for non-walk-in customers

---

## E) Decision

**Status: PARTIAL - Most exists, minor integration needed**

The Customer Module is **substantially complete**:
- Core entities exist: `Customer`, `CustomerLedgerEntry`, `Invoice`, `Cash`
- Service layer exists: `ICustomerLedgerService` with full implementation
- POS has customer selection with default customer logic
- Soft delete mechanism in place

**What needs verification/integration:**
1. POSViewModel's `CompletePayment` must call `RecordInvoiceEntries()` and `RecordCashMovement()`
2. Customer Statement view for displaying ledger entries
3. Customer Payment view for recording payments

---

## F) Minimal Plan (file-by-file)

### Phase 1: Verify POS Integration (Existing Files)
| File | Action |
|------|--------|
| `POS\ViewModels\POSViewModel.cs` | Verify/add ledger integration in `CompletePayment` |

### Phase 2: Customer Statement UI (May Need New)
| File | Action |
|------|--------|
| `POS\CustomControl\CustomerStatement_UserControl.xaml` | Create if not exists |
| `POS\ViewModels\CustomerStatementViewModel.cs` | Create if not exists |

### Phase 3: Customer Payment UI (May Need New)
| File | Action |
|------|--------|
| `POS\CustomControl\CustomerPayment_UserControl.xaml` | Create if not exists |
| `POS\ViewModels\CustomerPaymentViewModel.cs` | Create if not exists |

### Files NOT to Create (Already Exist):
- Customer.cs (exists)
- CustomerLedgerEntry.cs (exists)
- ICustomerLedgerService.cs (exists)
- CustomerLedgerService.cs (exists)
- Customer_Add_UserControl.xaml (exists)
- Customers_UserControl.xaml (exists)

---

## G) Navigation/Architecture Findings (for HomeWindow Refactor)

### Current Navigation Pattern
- **HomeWindow.xaml.cs**: Uses switch/case `CreateControlForMenuItem()`
- **No NavigationService**: Direct UserControl instantiation
- **No PageFactory**: Manual control creation
- **No TabManager**: Tab management in code-behind

### TreeView Navigation Issue
- Multiple event triggers may cause double navigation
- Need to consolidate to single event handler

### Performance Issues Identified
1. ViewModels load data in constructors (blocking UI)
2. No async loading pattern
3. No virtualization in some DataGrids
4. No cancellation tokens for DB operations

---

## Document Status

- **Created**: 2026-01-02
- **Author**: Claude Code Agent
- **Phase**: Discovery Complete
- **Next Step**: Proceed to Implementation Phase
