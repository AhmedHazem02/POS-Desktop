using POS.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace POS.CustomControl
{
    /// <summary>
    /// Interaction logic for CustomerLedger_UserControl.xaml
    /// </summary>
    public partial class CustomerLedger_UserControl : UserControl
    {
        private readonly CustomerLedgerViewModel viewModel;

        public CustomerLedger_UserControl()
        {
            InitializeComponent();
            viewModel = new CustomerLedgerViewModel();
            DataContext = viewModel;
            Loaded += CustomerLedger_UserControl_Loaded;
            Unloaded += CustomerLedger_UserControl_Unloaded;    
        }

        private async void CustomerLedger_UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await viewModel.InitializeAsync();
        }

        private void CustomerLedger_UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            viewModel.CancelPending();
        }
    }
}
