using LiveCharts;
using LiveCharts.Wpf;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using POS.Persistence.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace POS.ViewModels
{
    public class SummaryCard
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public PackIconKind Icon { get; set; }
        public string Color { get; set; }
    }

    public class DashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly AppDbContext _context;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _salesInvoiceCount;
        public int SalesInvoiceCount
        {
            get => _salesInvoiceCount;
            set { _salesInvoiceCount = value; OnPropertyChanged(nameof(SalesInvoiceCount)); UpdateSummaryCards(); }
        }

        private int _purchaseInvoiceCount;
        public int PurchaseInvoiceCount
        {
            get => _purchaseInvoiceCount;
            set { _purchaseInvoiceCount = value; OnPropertyChanged(nameof(PurchaseInvoiceCount)); UpdateSummaryCards(); }
        }

        private int _userCount;
        public int UserCount
        {
            get => _userCount;
            set { _userCount = value; OnPropertyChanged(nameof(UserCount)); UpdateSummaryCards(); }
        }

        private int _productCount;
        public int ProductCount
        {
            get => _productCount;
            set { _productCount = value; OnPropertyChanged(nameof(ProductCount)); UpdateSummaryCards(); }
        }

        private List<SummaryCard> _summaryCards;
        public List<SummaryCard> SummaryCards
        {
            get => _summaryCards;
            set { _summaryCards = value; OnPropertyChanged(nameof(SummaryCards)); }
        }

        private SeriesCollection _salesSeriesCollection;
        public SeriesCollection SalesSeriesCollection
        {
            get => _salesSeriesCollection;
            set { _salesSeriesCollection = value; OnPropertyChanged(nameof(SalesSeriesCollection)); }
        }

        private SeriesCollection _productInventorySeriesCollection;
        public SeriesCollection ProductInventorySeriesCollection
        {
            get => _productInventorySeriesCollection;
            set { _productInventorySeriesCollection = value; OnPropertyChanged(nameof(ProductInventorySeriesCollection)); }
        }

        private string[] _arabicLabels;
        public string[] ArabicLabels
        {
            get => _arabicLabels;
            set { _arabicLabels = value; OnPropertyChanged(nameof(ArabicLabels)); }
        }

        public Func<double, string> Formatter { get; set; }

        public DashboardViewModel(AppDbContext context)
        {
            _context = context;
            Formatter = value => value.ToString("N");
            
            // Initialize collections to avoid null reference issues before data loads
            SalesSeriesCollection = new SeriesCollection();
            ProductInventorySeriesCollection = new SeriesCollection();
            SummaryCards = new List<SummaryCard>();

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Summary Counts
                SalesInvoiceCount = await _context.Invoices.CountAsync();
                PurchaseInvoiceCount = await _context.Purchases.CountAsync();
                UserCount = await _context.Users.Where(u => u.FirstName != "System").CountAsync();
                ProductCount = await _context.Products.CountAsync();

                // Sales Chart (Last 7 days)
                var today = DateTime.Today;
                var sevenDaysAgo = today.AddDays(-6);
                var salesData = await _context.Invoices
                    .Where(i => i.Date.Date >= sevenDaysAgo && i.Date.Date <= today)
                    .GroupBy(i => i.Date.Date)
                    .Select(g => new { Date = g.Key, Total = g.Sum(i => i.TotalPrice) })
                    .ToListAsync();

                var salesValues = new ChartValues<decimal>();
                var dateLabels = new List<string>();
                for (int i = 0; i < 7; i++)
                {
                    var date = sevenDaysAgo.AddDays(i);
                    var sale = salesData.FirstOrDefault(s => s.Date == date);
                    salesValues.Add(sale?.Total ?? 0);
                    dateLabels.Add(date.ToString("ddd")); // Short day name e.g. Mon
                }

                SalesSeriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "المبيعات",
                        Values = salesValues,
                        PointGeometry = null
                    }
                };
                ArabicLabels = dateLabels.ToArray();

                // Product Inventory Chart (Top 5 by stock)
                var allProducts = await _context.Products
                                    .Include(p => p.PurchaseProducts)
                                    .Include(p => p.SaleProducts)
                                    .ToListAsync();

                var topProducts = allProducts.OrderByDescending(p => p.Quantity())
                                             .Take(5)
                                             .ToList();

                var pieSeriesCollection = new SeriesCollection();
                foreach (var product in topProducts)
                {
                    pieSeriesCollection.Add(new PieSeries
                    {
                        Title = product.Name,
                        Values = new ChartValues<double> { product.Quantity() },
                        DataLabels = false,
                    });
                }
                ProductInventorySeriesCollection = pieSeriesCollection;
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., database connection error)
                // For now, we can show a message or log it.
                MessageBox.Show($"حدث خطأ أثناء تحميل بيانات لوحة التحكم: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaryCards()
        {
            SummaryCards = new List<SummaryCard>
            {
                new SummaryCard { Title = "فواتير البيع", Value = SalesInvoiceCount.ToString(), Icon = PackIconKind.Receipt, Color = "#4CAF50" },
                new SummaryCard { Title = "فواتير الشراء", Value = PurchaseInvoiceCount.ToString(), Icon = PackIconKind.Cart, Color = "#2196F3" },
                new SummaryCard { Title = "عدد المستخدمين", Value = UserCount.ToString(), Icon = PackIconKind.AccountGroup, Color = "#FFC107" },
                new SummaryCard { Title = "عدد المنتجات", Value = ProductCount.ToString(), Icon = PackIconKind.Archive, Color = "#E91E63" }
            };
        }
    }
}
