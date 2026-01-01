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
        private readonly AddCustomersDialogViewModel viewModel;

        public Customers_UserControl()
        {
            InitializeComponent();
            viewModel = new AddCustomersDialogViewModel();
            DataContext = viewModel;
        }

        private async void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current?.MainWindow is HomeWindow homeWindow)
            {
                await homeWindow.OpenMenuItemByIdentifierAsync("addCustomer");
            }
        }
    }
}
