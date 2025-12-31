A) Project Map
- MVVM framework: Custom MVVM (INotifyPropertyChanged + RelayCommand/ViewModelCommand + ViewModelBase in POS/ViewModels).
- DataAccess: EF Core 8 + SQL Server via POS.Persistence/Context/AppDbContext.cs (IdentityDbContext).
- Key folders: POS/Views, POS/CustomControl, POS/Dialogs, POS/ViewModels, POS.Domain/Models, POS.Persistence/Context, POS.Persistence/Migrations, POS.Application/Contracts, POS.Infrustructure/Services.

B) Existing Components (with paths)
- Customer entity/model: POS.Domain/Models/Customer.cs; DbSet in POS.Persistence/Context/AppDbContext.cs.
- Customer list/details UI: POS/CustomControl/Customer_Add_UserControl.xaml; POS/ViewModels/AddCustomersDialogViewModel.cs; POS/ViewModels/AddOrEditPersonViewModel.cs; POS/Dialogs/AddCustomersDialog.xaml; POS/Views/CustomersWindow.xaml (empty grid).
- POS/Sale/Invoice workflow: POS/CustomControl/POS_UserControl.xaml; POS/ViewModels/POSViewModel.cs; POS.Domain/Models/Invoice.cs; POS.Domain/Models/Products/SaleProduct.cs; POS.Domain/Models/Payments/InvoicePayment.cs; POS/ViewModels/SalesHistoryViewModel.cs; POS/CustomControl/SalesHistory_UserControl.xaml.
- Payment/Cash workflow: POS/Dialogs/PaymentDialog.xaml; POS/ViewModels/PaymentDialogViewModel.cs; POS.Domain/Models/Payments/InvoicePayment.cs; POS.Domain/Models/Payments/PaymentMethods/{Cash,Bank,Cheque,CreditCard}.cs; POS.Domain/Models/E-Invoice/PaymentMethod.cs.
- Ledger/Statement/AR: No ledger/statement models or services found. Only menu identifiers (customerLedger, supplierLedger, generalLedger, trialBalance) in POS/Views/HomeWindow.xaml.cs.
- Soft delete mechanism: Not found. AddOrEditPersonViewModel.cs deletes via _dbContext.Set<T>().Remove(...) with SaveChanges().

C) Discovered Naming (names discovered)
- Entity name for customer: Customer (POS.Domain.Models.Customer).
- Default/Walk-in customer name (if found): Not found (no WalkIn/DefaultCustomer strings; no customer seed in migrations).
- Service names used for sales: None; logic is in POS/ViewModels/POSViewModel.cs and POS/ViewModels/Base/BaseProductsViewModel.cs.
- Service names used for payments/cash: None; POS writes InvoicePayment directly in POS/ViewModels/POSViewModel.cs.
- Existing enums/types for payment methods: PaymentType enum in POS.Domain/Models/Payments/InvoicePayment.cs (Cash, CreditCard, BankTransfer, Cheque, Other, OnAccount); TransactionType/CurrencyType enums in POS.Domain/Models/Payments/PaymentMethods/Cash.cs; POS.Domain.Models.E_Invoice.PaymentMethod entity.
- Invoice numbering scheme: GetNextInvoiceNumber() in POS/ViewModels/POSViewModel.cs uses prefix "INV-" and 3-digit sequence.

D) Gaps
- No default/walk-in customer seed or ensure logic.
- POS UI has no customer selector (no Customer binding in POS/CustomControl/POS_UserControl.xaml) even though BaseProductsViewModel exposes Customers/SelectedCustomer.
- No ledger/statement entity or service to compute running balance; report menu entries are not wired to a view.
- No dedicated customer payment workflow (separate from invoice) and no link between customer selection and payment in POS flow.
- Cash movement tracking is not implemented; Cash entity exists but not used by POS when saving invoice payments.
- Customer deletion is hard delete; no archive/soft delete flag or flow.

E) Decision
- Partial. Core Customer/Invoice/InvoicePayment entities and POS sale flow exist, but default customer, ledger/statement, customer payments, and soft delete are missing.
- Decision logic: extend existing POS/Invoice/Payment flow using current entities and UI wiring; add only the missing ledger/statement and customer payment flow with a single shared service for business logic; introduce soft delete/archiving for Customer and update queries to respect it. No duplicate payment enums or parallel services.

F) Minimal Plan (file-by-file)
- POS/ViewModels/Base/BaseProductsViewModel.cs: load/select default customer (once discovered or ensured) and keep SelectedCustomer consistent.
- POS/CustomControl/POS_UserControl.xaml: add customer selection bound to Customers/SelectedCustomer.
- POS/ViewModels/POSViewModel.cs: persist SelectedCustomer on Invoice; handle paid vs due; create ledger entries and payment records using existing types.
- POS/Dialogs/PaymentDialog.xaml + POS/ViewModels/PaymentDialogViewModel.cs: reuse or extend for customer payment input if applicable (avoid new dialog if not needed).
- POS/ViewModels/AddOrEditPersonViewModel.cs: replace hard delete with archive/soft delete after a proper flag is introduced.
- POS/Views/HomeWindow.xaml.cs: wire a Customer Statement view when it exists.
- New files only if required (no existing ledger components): add a single ledger entry model + service (names TBD based on repo naming) and a minimal statement View/ViewModel; add DbSet + migration to AppDbContext if ledger is persisted.

G) Changes Applied
- Added ledger entity + DbSet + service for customer statement and running balance: POS.Domain/Models/CustomerLedgerEntry.cs; POS.Persistence/Context/AppDbContext.cs; POS.Application/Contracts/Services/ICustomerLedgerService.cs; POS.Infrustructure/Services/CustomerLedgerService.cs.
- Added default/walk-in handling + archive flags: POS.Domain/Models/Customer.cs; POS/ViewModels/Base/BaseProductsViewModel.cs; POS/ViewModels/AddOrEditPersonViewModel.cs.
- POS integration for customer selection + due handling + ledger/cash recording: POS/CustomControl/POS_UserControl.xaml; POS/ViewModels/POSViewModel.cs.
- Customer statement + payment UI + wiring: POS/CustomControl/CustomerLedger_UserControl.xaml; POS/CustomControl/CustomerLedger_UserControl.xaml.cs; POS/ViewModels/CustomerLedgerViewModel.cs; POS/Views/HomeWindow.xaml.cs.
- Service fix for accurate running balance with date filters (opening balance used when no entries): POS.Infrustructure/Services/CustomerLedgerService.cs; POS/ViewModels/CustomerLedgerViewModel.cs.
- Ledger payment method now reflects actual payment on partial sales: POS.Application/Contracts/Services/ICustomerLedgerService.cs; POS.Infrustructure/Services/CustomerLedgerService.cs; POS/ViewModels/POSViewModel.cs.
- Customer UI live search + POS auto-refresh after add/edit/archive: POS/CustomControl/Customer_Add_UserControl.xaml; POS/ViewModels/AddOrEditPersonViewModel.cs; POS/ViewModels/Base/BaseProductsViewModel.cs; POS/App.xaml.cs.
- Added EF migration for ledger + customer archive/default: POS.Persistence/Migrations/20251231105512_AddCustomerLedgerAndArchive.cs (+ Designer) and updated snapshot.

H) Final List of Modified Files
- POS.Application/Contracts/Services/ICustomerLedgerService.cs
- POS/CustomControl/CustomerLedger_UserControl.xaml
- POS/CustomControl/CustomerLedger_UserControl.xaml.cs
- POS/CustomControl/POS_UserControl.xaml
- POS.Domain/Models/Customer.cs
- POS.Domain/Models/CustomerLedgerEntry.cs
- POS.Infrustructure/InfrustructureDependencies.cs
- POS.Infrustructure/Services/CustomerLedgerService.cs
- POS.Infrustructure/POS.Infrustructure.csproj
- POS.Persistence/Context/AppDbContext.cs
- POS.Persistence/Migrations/20251231105512_AddCustomerLedgerAndArchive.cs
- POS.Persistence/Migrations/20251231105512_AddCustomerLedgerAndArchive.Designer.cs
- POS.Persistence/Migrations/AppDbContextModelSnapshot.cs
- POS/App.xaml.cs
- POS/ViewModels/AddOrEditPersonViewModel.cs
- POS/ViewModels/Base/BaseProductsViewModel.cs
- POS/ViewModels/CustomerLedgerViewModel.cs
- POS/ViewModels/POSViewModel.cs
- POS/CustomControl/Customer_Add_UserControl.xaml
- POS/Views/HomeWindow.xaml.cs

I) Notes / Remaining
- Apply migration to the database (Update-Database or `dotnet ef database update`).
