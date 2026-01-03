using LiveCharts;
using LiveCharts.Wpf;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using POS.Persistence.Context;
using POS.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace POS.ViewModels
{
    public class QuickAccessCard
    {
        public string Title { get; set; } = "";
        public string Identifier { get; set; } = "";
        public PackIconKind Icon { get; set; }
        public Brush GradientBrush { get; set; } = Brushes.Blue;
        public bool IsVisible { get; set; } = true;
        public string RequiredClaim { get; set; } = "";
    }

    public class SummaryCard
    {
        public string Title { get; set; } = "";
        public string Value { get; set; } = "";
        public PackIconKind Icon { get; set; }
        public string Color { get; set; } = "";
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly AppDbContext _context;
        private readonly AuthenticationService _authService;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // User Info
        private string _userName = "";
        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(nameof(UserName)); }
        }

        private string _userRole = "";
        public string UserRole
        {
            get => _userRole;
            set { _userRole = value; OnPropertyChanged(nameof(UserRole)); }
        }

        private string _currentDate = "";
        public string CurrentDate
        {
            get => _currentDate;
            set { _currentDate = value; OnPropertyChanged(nameof(CurrentDate)); }
        }

        // Statistics
        private decimal _todaySales;
        public decimal TodaySales
        {
            get => _todaySales;
            set { _todaySales = value; OnPropertyChanged(nameof(TodaySales)); }
        }

        private int _salesInvoiceCount;
        public int SalesInvoiceCount
        {
            get => _salesInvoiceCount;
            set { _salesInvoiceCount = value; OnPropertyChanged(nameof(SalesInvoiceCount)); }
        }

        private int _productCount;
        public int ProductCount
        {
            get => _productCount;
            set { _productCount = value; OnPropertyChanged(nameof(ProductCount)); }
        }

        private int _customerCount;
        public int CustomerCount
        {
            get => _customerCount;
            set { _customerCount = value; OnPropertyChanged(nameof(CustomerCount)); }
        }

        // Notifications
        private bool _hasNotifications = true;
        public bool HasNotifications
        {
            get => _hasNotifications;
            set { _hasNotifications = value; OnPropertyChanged(nameof(HasNotifications)); }
        }

        private int _notificationCount = 3;
        public int NotificationCount
        {
            get => _notificationCount;
            set { _notificationCount = value; OnPropertyChanged(nameof(NotificationCount)); }
        }

        // Quick Access Cards
        private ObservableCollection<QuickAccessCard> _quickAccessCards = new();
        public ObservableCollection<QuickAccessCard> QuickAccessCards
        {
            get => _quickAccessCards;
            set { _quickAccessCards = value; OnPropertyChanged(nameof(QuickAccessCards)); }
        }

        // Commands
        public ICommand NavigateCommand { get; }
        public ICommand RefreshCommand { get; }

        // Gradient Brushes
        private static LinearGradientBrush CreateGradient(string color1, string color2)
        {
            return new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop((Color)ColorConverter.ConvertFromString(color1), 0),
                    new GradientStop((Color)ColorConverter.ConvertFromString(color2), 1)
                }
            };
        }

        public DashboardViewModel(AppDbContext context)
        {
            _context = context;
            _authService = App.ServiceProvider.GetRequiredService<AuthenticationService>();

            NavigateCommand = new RelayCommand(param => ExecuteNavigate(param as string));
            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());

            InitializeUserInfo();
            InitializeQuickAccessCards();
            _ = LoadDataAsync();
        }

        private void InitializeUserInfo()
        {
            if (_authService.CurrentUser != null)
            {
                UserName = _authService.CurrentUser.FullName ?? "مستخدم";
                UserRole = _authService.CurrentUser.DefaultRole ?? "مستخدم";
            }
            else
            {
                UserName = "مستخدم";
                UserRole = "مستخدم";
            }

            var arabicCulture = new CultureInfo("ar-EG");
            CurrentDate = DateTime.Now.ToString("dddd، d MMMM yyyy", arabicCulture);
        }

        private void InitializeQuickAccessCards()
        {
            var isAdmin = _authService.CurrentUser?.DefaultRole == "Administrator";

            var allCards = new List<QuickAccessCard>
            {
                new QuickAccessCard
                {
                    Title = "الكاشير",
                    Identifier = "salesScreen",
                    Icon = PackIconKind.CashRegister,
                    GradientBrush = CreateGradient("#1E40AF", "#3B82F6"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "فواتير البيع",
                    Identifier = "salesInvoices",
                    Icon = PackIconKind.Receipt,
                    GradientBrush = CreateGradient("#2563EB", "#0EA5E9"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "فاتورة شراء",
                    Identifier = "purchaseGoods",
                    Icon = PackIconKind.Cart,
                    GradientBrush = CreateGradient("#0891B2", "#06B6D4"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "المخزن",
                    Identifier = "inventory",
                    Icon = PackIconKind.Package,
                    GradientBrush = CreateGradient("#7C3AED", "#A855F7"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "العملاء",
                    Identifier = "customers",
                    Icon = PackIconKind.AccountGroup,
                    GradientBrush = CreateGradient("#059669", "#10B981"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "الموردين",
                    Identifier = "suppliers",
                    Icon = PackIconKind.TruckDelivery,
                    GradientBrush = CreateGradient("#EA580C", "#F97316"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "عرض سعر",
                    Identifier = "addPriceOffer",
                    Icon = PackIconKind.TagOutline,
                    GradientBrush = CreateGradient("#DC2626", "#EF4444"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "تقرير الخزينة",
                    Identifier = "treasuryReport",
                    Icon = PackIconKind.ChartBar,
                    GradientBrush = CreateGradient("#0D9488", "#14B8A6"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "كشف حساب عميل",
                    Identifier = "customerLedger",
                    Icon = PackIconKind.FileDocument,
                    GradientBrush = CreateGradient("#4F46E5", "#6366F1"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "نقل المنتجات",
                    Identifier = "goodsMovement",
                    Icon = PackIconKind.TruckFast,
                    GradientBrush = CreateGradient("#BE185D", "#EC4899"),
                    IsVisible = true
                },
                new QuickAccessCard
                {
                    Title = "المستخدمين",
                    Identifier = "accountSettings",
                    Icon = PackIconKind.AccountMultiple,
                    GradientBrush = CreateGradient("#1E40AF", "#0EA5E9"),
                    IsVisible = isAdmin
                },
                new QuickAccessCard
                {
                    Title = "الإعدادات",
                    Identifier = "companySettings",
                    Icon = PackIconKind.Cog,
                    GradientBrush = CreateGradient("#475569", "#64748B"),
                    IsVisible = isAdmin
                }
            };

            // Only add visible cards to the collection
            QuickAccessCards = new ObservableCollection<QuickAccessCard>(allCards.Where(c => c.IsVisible));
        }

        private void ExecuteNavigate(string? identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return;

            try
            {
                // Find the HomeWindow and navigate to the menu item
                var homeWindow = System.Windows.Application.Current.Windows
                    .OfType<HomeWindow>()
                    .FirstOrDefault();

                if (homeWindow != null)
                {
                    _ = homeWindow.OpenMenuItemByIdentifierAsync(identifier);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التنقل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var today = DateTime.Today;

                // Today's sales
                TodaySales = await _context.Invoices
                    .Where(i => i.Date.Date == today)
                    .SumAsync(i => i.TotalPrice);

                // Total sales invoices count
                SalesInvoiceCount = await _context.Invoices.CountAsync();

                // Total products count
                ProductCount = await _context.Products.CountAsync();

                // Total customers count
                CustomerCount = await _context.Customers.CountAsync();

                // Low stock notifications (products with quantity < 10)
                var lowStockCount = await _context.Products
                    .Include(p => p.PurchaseProducts)
                    .Include(p => p.SaleProducts)
                    .CountAsync();

                // For now, set notification count based on some logic
                NotificationCount = 3;
                HasNotifications = NotificationCount > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
