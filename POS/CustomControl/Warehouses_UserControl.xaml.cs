using Microsoft.EntityFrameworkCore;
using POS.Dialogs;
using POS.Domain.Models;
using POS.Persistence.Context;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace POS.CustomControl
{
    public partial class Warehouses_UserControl : UserControl
    {
        private AppDbContext _dbContext;
        private ObservableCollection<Warehouse> _warehouses;
        private CollectionViewSource _warehousesViewSource;

        public Warehouses_UserControl()
        {
            InitializeComponent();
            _dbContext = new AppDbContext();
            _warehousesViewSource = new CollectionViewSource();
            LoadWarehouses();
        }

        private void LoadWarehouses()
        {
            var warehouses = _dbContext.Warehouses.ToList();
            _warehouses = new ObservableCollection<Warehouse>(warehouses);
            _warehousesViewSource.Source = _warehouses;
            WarehousesDataGrid.ItemsSource = _warehousesViewSource.View;
        }

        private void AddWarehouse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddWarhousesDialog();
                dialog.viewModel.IsEdit = false;
                dialog.ShowDialog();
                
                if (dialog.viewModel.IsSaved)
                {
                    LoadWarehouses();
                    App.NotifyWarehousesChanged();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح النافذة: {ex.Message}\n\n{ex.StackTrace}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditWarehouse_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Warehouse warehouse)
            {
                var dialog = new AddWarhousesDialog();
                dialog.viewModel.IsEdit = true;
                dialog.viewModel.LoadWarehouseData(warehouse.Id);
                dialog.ShowDialog();
                
                if (dialog.viewModel.IsSaved)
                {
                    LoadWarehouses();
                    App.NotifyWarehousesChanged();
                }
            }
        }

        private void DeleteWarehouse_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Warehouse warehouse)
            {
                var result = MessageBox.Show(
                    $"هل أنت متأكد من حذف المخزن '{warehouse.Name}'؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var warehouseToDelete = _dbContext.Warehouses.Find(warehouse.Id);
                        if (warehouseToDelete != null)
                        {
                            _dbContext.Warehouses.Remove(warehouseToDelete);
                            _dbContext.SaveChanges();
                            LoadWarehouses();
                            App.NotifyWarehousesChanged();
                            MessageBox.Show("تم حذف المخزن بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _warehousesViewSource.View.Filter = null;
            }
            else
            {
                _warehousesViewSource.View.Filter = item =>
                {
                    if (item is Warehouse warehouse)
                    {
                        return warehouse.Name.ToLower().Contains(searchText) ||
                               (warehouse.Location?.ToLower().Contains(searchText) ?? false);
                    }
                    return false;
                };
            }
        }
    }
}
