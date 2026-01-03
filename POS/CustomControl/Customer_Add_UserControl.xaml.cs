using POS.Domain.Models;
using POS.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace POS.CustomControl
{
    /// <summary>
    /// Interaction logic for Customer_Add_UserControl.xaml
    /// </summary>
    public partial class Customer_Add_UserControl : UserControl
    {
        public AddCustomersDialogViewModel viewModel;

        /// <summary>
        /// Static field to pass a customer for editing from the customers list page
        /// </summary>
        public static Customer? CustomerToEdit { get; set; }

        public Customer_Add_UserControl()
        {
            InitializeComponent();
            viewModel = new AddCustomersDialogViewModel();
            DataContext = viewModel;
            Loaded += Customer_Add_UserControl_Loaded;
        }

        private void Customer_Add_UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Check if there's a customer to edit
            if (CustomerToEdit != null)
            {
                viewModel.SelectedItem = CustomerToEdit;
                CustomerToEdit = null; // Clear it after use
            }
        }
    }
}
