using Microsoft.Extensions.DependencyInjection;
using POS.Application.Contracts.Services;
using POS.Infrustructure.Services;
using POS.ViewModels;
using System;
using System.Windows.Controls;
using System.Windows.Data;

namespace POS.CustomControl
{
    /// <summary>
    /// Interaction logic for Inventory_UserControl.xaml
    /// </summary>
    public partial class Inventory_UserControl : UserControl
    {
        private InventoryViewModel viewModel;

        public Inventory_UserControl()
        {
            InitializeComponent();
            var excelService = App.ServiceProvider?.GetService<IExcelService>() ?? new ExcelService();
            viewModel = new InventoryViewModel(excelService);
            DataContext = viewModel;
        }
        private void Window_Closed(object sender, EventArgs e)
        {

        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty).UpdateSource();
            }
        }

        private void ProductsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}
