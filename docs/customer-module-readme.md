# Customer Module Changes (Codex Chat)

This document summarizes the changes made in this chat to implement the Customer/Client process in the POS app.

## Scope
- Discovery and audit completed in `docs/customer-module-audit.md`.
- Customer selection in POS, default customer handling, ledger/statement, customer payments, and cash movement wiring.
- Soft delete (archive) for customers.

## Data Model
- Added `CustomerLedgerEntry` entity with debit/credit, reference, and running balance (not persisted).
  - File: `POS.Domain/Models/CustomerLedgerEntry.cs`
- Added `IsArchived` and `IsDefault` flags on `Customer`.
  - File: `POS.Domain/Models/Customer.cs`

## Services and Business Logic
- Added customer ledger service interface and implementation.
  - Files: `POS.Application/Contracts/Services/ICustomerLedgerService.cs`, `POS.Infrustructure/Services/CustomerLedgerService.cs`
- Default customer is ensured once and reused (name is `Walk-in`).
- Ledger statement computes running balance, including opening balance for filtered ranges.
- Invoice ledger entries now record invoice method and actual payment method separately on partial payments.
- Customer payment posts a ledger credit and optional cash movement.

## POS Integration
- POS view binds customer selection and blocks "due" on default customer.
  - Files: `POS/CustomControl/POS_UserControl.xaml`, `POS/ViewModels/POSViewModel.cs`
- On sale save:
  - Invoice uses selected customer.
  - Paid amount posts `InvoicePayment` and cash movement.
  - Due posts ledger debit (non-default customers only).

## Customer Statement + Payment UI
- Added customer statement user control with filters and a payment entry section.
  - Files: `POS/CustomControl/CustomerLedger_UserControl.xaml`, `POS/CustomControl/CustomerLedger_UserControl.xaml.cs`
- Added view model to load customers, statements, and post payments.
  - File: `POS/ViewModels/CustomerLedgerViewModel.cs`
- Wired report menu item `customerLedger` to the new statement control.
  - File: `POS/Views/HomeWindow.xaml.cs`

## Customer UI Enhancements
- Added live search in the customer screen (search by name or phone).
  - Files: `POS/CustomControl/Customer_Add_UserControl.xaml`, `POS/ViewModels/AddOrEditPersonViewModel.cs`
- Customer list in POS refreshes automatically after add/edit/archive.
  - Files: `POS/App.xaml.cs`, `POS/ViewModels/Base/BaseProductsViewModel.cs`, `POS/ViewModels/AddOrEditPersonViewModel.cs`

## Soft Delete / Archive
- Customer delete now archives instead of hard delete.
- Default customer cannot be deleted.
  - File: `POS/ViewModels/AddOrEditPersonViewModel.cs`

## Persistence
- Added DbSet for ledger entries and migration.
  - Files: `POS.Persistence/Context/AppDbContext.cs`, `POS.Persistence/Migrations/20251231105512_AddCustomerLedgerAndArchive.cs`,
    `POS.Persistence/Migrations/20251231105512_AddCustomerLedgerAndArchive.Designer.cs`,
    `POS.Persistence/Migrations/AppDbContextModelSnapshot.cs`

## Dependency Injection
- Registered ledger service.
  - File: `POS.Infrustructure/InfrustructureDependencies.cs`
- Added persistence reference to infrastructure project.
  - File: `POS.Infrustructure/POS.Infrustructure.csproj`

## Tests Run
- `dotnet build POS.sln` (succeeded with existing warnings).

## Files Added
- `POS.Domain/Models/CustomerLedgerEntry.cs`
- `POS.Application/Contracts/Services/ICustomerLedgerService.cs`
- `POS.Infrustructure/Services/CustomerLedgerService.cs`
- `POS/CustomControl/CustomerLedger_UserControl.xaml`
- `POS/CustomControl/CustomerLedger_UserControl.xaml.cs`
- `POS/ViewModels/CustomerLedgerViewModel.cs`
- `POS.Persistence/Migrations/20251231105512_AddCustomerLedgerAndArchive.cs`
- `POS.Persistence/Migrations/20251231105512_AddCustomerLedgerAndArchive.Designer.cs`

## Files Modified
- `POS/App.xaml.cs`
- `POS.Domain/Models/Customer.cs`
- `POS.Persistence/Context/AppDbContext.cs`
- `POS.Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `POS.Infrustructure/InfrustructureDependencies.cs`
- `POS.Infrustructure/POS.Infrustructure.csproj`
- `POS/ViewModels/Base/BaseProductsViewModel.cs`
- `POS/ViewModels/AddOrEditPersonViewModel.cs`
- `POS/ViewModels/POSViewModel.cs`
- `POS/CustomControl/POS_UserControl.xaml`
- `POS/CustomControl/Customer_Add_UserControl.xaml`
- `POS/Views/HomeWindow.xaml.cs`
- `docs/customer-module-audit.md`

## Follow-up
- Apply migration to the database: `dotnet ef database update`.
