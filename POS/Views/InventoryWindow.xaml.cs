using Microsoft.Extensions.DependencyInjection;
using POS.Application.Contracts.Services;
using POS.Infrustructure.Services;
using POS.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace POS.Views
{
    /// <summary>
    /// Interaction logic for InventoryWindow.xaml
    /// </summary>
    public partial class InventoryWindow : Window
    {
        private InventoryViewModel viewModel;

        public InventoryWindow()
        {
            InitializeComponent();
            var excelService = App.ServiceProvider?.GetService<IExcelService>() ?? new ExcelService();
            viewModel = new InventoryViewModel(excelService);
            DataContext = viewModel;
        }
        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
