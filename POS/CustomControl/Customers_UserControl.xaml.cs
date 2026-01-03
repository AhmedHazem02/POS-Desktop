using POS.Domain.Models;
using POS.ViewModels;
using POS.Views;
using System.Windows;
using System.Windows.Controls;

namespace POS.CustomControl
{
    /// <summary>
    /// Interaction logic for Customers_UserControl.xaml
    /// </summary>
    public partial class Customers_UserControl : UserControl
    {
        private readonly CustomersPageViewModel viewModel;

        public Customers_UserControl()
        {
            InitializeComponent();
            viewModel = new CustomersPageViewModel();
            DataContext = viewModel;
            Loaded += Customers_UserControl_Loaded;
            Unloaded += Customers_UserControl_Unloaded;
        }

        private void Customers_UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.CustomersChanged += OnCustomersChanged;
            viewModel.RefreshItems();
        }

        private void Customers_UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            App.CustomersChanged -= OnCustomersChanged;
        }

        private void OnCustomersChanged()
        {
            viewModel.RefreshItems();
        }

        private async void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is HomeWindow homeWindow)
            {
                await homeWindow.OpenMenuItemByIdentifierAsync("addCustomer");
            }
        }
    }
}
