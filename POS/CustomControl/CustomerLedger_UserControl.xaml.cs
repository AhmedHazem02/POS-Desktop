using POS.ViewModels;
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
        }
    }
}
