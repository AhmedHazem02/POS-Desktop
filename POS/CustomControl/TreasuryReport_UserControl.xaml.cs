using POS.ViewModels;
using System.Windows.Controls;

namespace POS.CustomControl
{
    /// <summary>
    /// Interaction logic for TreasuryReport_UserControl.xaml
    /// </summary>
    public partial class TreasuryReport_UserControl : UserControl
    {
        private readonly TreasuryReportViewModel viewModel;

        public TreasuryReport_UserControl()
        {
            InitializeComponent();
            viewModel = new TreasuryReportViewModel();
            DataContext = viewModel;
        }
    }
}
