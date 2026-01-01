using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using POS.Application.Contracts.Services;
using POS.Domain.Models;
using POS.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace POS.ViewModels
{
    public abstract class AddOrEditPersonViewModel<T> : INotifyPropertyChanged where T : class, IPerson, new()
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected AppDbContext _dbContext;

        // Abstract property for the image folder path
        protected abstract string ImageFolderName { get; }

        #region Properties
        private bool? _result;
        public bool? Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(nameof(Result)); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        private string _contactName;
        public string ContactName
        {
            get { return _contactName; }
            set { if (_contactName != value) { _contactName = value; OnPropertyChanged(nameof(ContactName)); } }
        }

        private string _email;
        public string Email
        {
            get { return _email; }
            set { if (_email != value) { _email = value; OnPropertyChanged(nameof(Email)); } }
        }

        private string _phone;
        public string Phone
        {
            get { return _phone; }
            set { if (_phone != value) { _phone = value; OnPropertyChanged(nameof(Phone)); } }
        }

        private string _address;
        public string Address
        {
            get { return _address; }
            set { if (_address != value) { _address = value; OnPropertyChanged(nameof(Address)); } }
        }

        private string _city;
        public string City
        {
            get { return _city; }
            set { if (_city != value) { _city = value; OnPropertyChanged(nameof(City)); } }
        }

        private string _country;
        public string Country
        {
            get { return _country; }
            set { if (_country != value) { _country = value; OnPropertyChanged(nameof(Country)); } }
        }

        private string _postalCode;
        public string PostalCode
        {
            get { return _postalCode; }
            set { if (_postalCode != value) { _postalCode = value; OnPropertyChanged(nameof(PostalCode)); } }
        }

        private string _website;
        public string Website
        {
            get { return _website; }
            set { if (_website != value) { _website = value; OnPropertyChanged(nameof(Website)); } }
        }

        private string _notes;
        public string Notes
        {
            get { return _notes; }
            set { if (_notes != value) { _notes = value; OnPropertyChanged(nameof(Notes)); } }
        }

        private string _commercialRegister;
        public string CommercialRegister
        {
            get { return _commercialRegister; }
            set { if (_commercialRegister != value) { _commercialRegister = value; OnPropertyChanged(nameof(CommercialRegister)); } }
        }

        private string _taxCard;
        public string TaxCard
        {
            get { return _taxCard; }
            set { if (_taxCard != value) { _taxCard = value; OnPropertyChanged(nameof(TaxCard)); } }
        }

        private string _imageSource;
        public string ImageSource
        {
            get { return _imageSource; }
            set { if (_imageSource != value) { _imageSource = value; OnPropertyChanged(nameof(ImageSource)); } }
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    LoadItems();
                }
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                    OnPropertyChanged(nameof(HasStatusMessage));
                }
            }
        }

        public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
        private ObservableCollection<T> _itemList;
        public ObservableCollection<T> ItemList
        {
            get { return _itemList; }
            set { if (_itemList != value) { _itemList = value; OnPropertyChanged(nameof(ItemList)); } }
        }

        private T _selectedItem;
        public T SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                    if (_selectedItem != null)
                    {
                        PopulateFieldsFromItem(_selectedItem);
                        CurrentState = DialogState.Modify;
                    }
                    else
                    {
                        ClearFields();
                        CurrentState = DialogState.Add;
                    }
                }
            }
        }
        #endregion

        #region Commands
        public ICommand AddCommand { get; private set; }
        public ICommand EditCommand { get; private set; }
        public ICommand SelectImageCommand { get; private set; }
        public ICommand EditItemCommand { get; private set; }
        public ICommand DeleteItemCommand { get; private set; }
        public ICommand FinishCommand { get; private set; }
        #endregion

        protected AddOrEditPersonViewModel()
        {
            _dbContext = new AppDbContext();
            
            AddCommand = new RelayCommand(Add);
            EditCommand = new RelayCommand(Edit);
            EditItemCommand = new RelayCommand(EditItem);
            SelectImageCommand = new RelayCommand(SelectImage);
            DeleteItemCommand = new RelayCommand(DeleteItem);
            FinishCommand = new RelayCommand(Finish);

            ItemList = new ObservableCollection<T>();
            LoadItems();
        }

        private void LoadItems()
        {
            List<T> items;
            if (typeof(T) == typeof(Customer))
            {
                var query = _dbContext.Set<Customer>().Where(c => !c.IsArchived);
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var term = SearchText.Trim();
                    query = query.Where(c =>
                        (c.Name != null && c.Name.Contains(term))
                        || (c.Phone != null && c.Phone.Contains(term)));
                }

                items = query.Cast<T>().ToList();
            }
            else
            {
                items = _dbContext.Set<T>().ToList();
            }

            ItemList.Clear();
            foreach (var item in items)
            {
                ItemList.Add(item);
            }
        }

        private void PopulateFieldsFromItem(T item)
        {
            Name = item.Name;
            ContactName = item.ContactName;
            Email = item.Email;
            Phone = item.Phone;
            Address = item.Address;
            City = item.City;
            Country = item.Country;
            PostalCode = item.PostalCode;
            Website = item.Website;
            Notes = item.Notes;
            CommercialRegister = item.CommercialRegister;
            TaxCard = item.TaxCard;
            ImageSource = item.Image;
        }

        private void ClearFields()
        {
            Name = string.Empty;
            ContactName = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Address = string.Empty;
            City = string.Empty;
            Country = string.Empty;
            PostalCode = string.Empty;
            Website = string.Empty;
            Notes = string.Empty;
            CommercialRegister = string.Empty;
            TaxCard = string.Empty;
            ImageSource = null;
        }
        
        private void Add(object parameter)
        {
            StatusMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("يرجى توفير اسم.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var nameExists = typeof(T) == typeof(Customer)
                ? _dbContext.Set<Customer>().Any(c => c.Name == Name && !c.IsArchived)
                : _dbContext.Set<T>().Any(c => c.Name == Name);

            if (nameExists)
            {
                MessageBox.Show("اسم موجود بالفعل.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newItem = new T();
            UpdateItemFromFields(newItem);
            
            _dbContext.Set<T>().Add(newItem);
            _dbContext.SaveChanges();

            if (typeof(T) == typeof(Customer))
            {
                LoadItems();
                StatusMessage = "تم إضافة عميل بنجاح.";
            }
            else
            {
                ItemList.Add(newItem);
            }

            ClearFields();

            NotifyCustomersChangedIfNeeded();
        }

        private void Edit(object parameter)
        {
            StatusMessage = string.Empty;
            if (SelectedItem != null)
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    MessageBox.Show("يرجى توفير اسم.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var nameExists = typeof(T) == typeof(Customer)
                ? _dbContext.Set<Customer>().Any(c => c.Name == Name && !c.IsArchived && c.Id != SelectedItem.Id)
                : _dbContext.Set<T>().Any(c => c.Name == Name && c.Id != SelectedItem.Id);

                if (nameExists)
                {
                    MessageBox.Show("اسم موجود بالفعل.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var itemToUpdate = _dbContext.Set<T>().Find(SelectedItem.Id);
                if(itemToUpdate != null)
                {
                    UpdateItemFromFields(itemToUpdate);
                    _dbContext.SaveChanges();

                    // Refresh item in the list
                    if (typeof(T) == typeof(Customer))
                    {
                        LoadItems();
                    }
                    else
                    {
                        var index = ItemList.IndexOf(SelectedItem);
                        ItemList[index] = itemToUpdate;
                    }
                    NotifyCustomersChangedIfNeeded();
                }
            }
        }

        private void DeleteItem(object parameter)
        {
            StatusMessage = string.Empty;
            if (SelectedItem == null)
            {
                return;
            }

            if (SelectedItem is Customer)
            {
                var ledgerService = App.ServiceProvider.GetRequiredService<ICustomerLedgerService>();
                var customer = _dbContext.Customers.Find(SelectedItem.Id);
                if (customer == null)
                {
                    return;
                }

                if (customer.IsDefault)
                {
                    MessageBox.Show("لا يمكن حذف العميل الافتراضي.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                customer.IsArchived = true;
                _dbContext.SaveChanges();

                NotifyCustomersChangedIfNeeded();

                ItemList.Remove(SelectedItem);
                ClearFields();

                if (ledgerService.HasCustomerTransactions(customer.Id))
                {
                    MessageBox.Show("تم أرشفة العميل لوجود معاملات.", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return;
            }

            MessageBoxResult result = MessageBox.Show("هل أنت متأكد من رغبتك في الحذف ؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                var itemToDelete = _dbContext.Set<T>().Find(SelectedItem.Id);
                if (itemToDelete != null)
                {
                    _dbContext.Set<T>().Remove(itemToDelete);
                    _dbContext.SaveChanges();
                    ItemList.Remove(SelectedItem);
                    ClearFields();
                }
            }
        }

        private void EditItem(object parameter)
        {
            if (parameter is T item)
            {
                SelectedItem = item;
            }
        }

        private void SelectImage(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                ImageSource = openFileDialog.FileName;
            }
        }

        public string SaveImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return string.Empty;
            }

            try
            {
                string uniqueFileName = $"{Guid.NewGuid()}.jpeg";
                string directoryPath = Path.Combine(Environment.CurrentDirectory, "images", ImageFolderName);
                string destinationImagePath = Path.Combine(directoryPath, uniqueFileName);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                using (var stream = File.OpenRead(imagePath))
                using (var image = Image.FromStream(stream))
                using (var bitmap = new Bitmap(image))
                {
                    bitmap.Save(destinationImagePath, ImageFormat.Jpeg);
                }
                return destinationImagePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void UpdateItemFromFields(T item)
        {
            item.Name = Name;
            item.ContactName = ContactName;
            item.Email = Email;
            item.Phone = Phone;
            item.Address = Address;
            item.City = City;
            item.Country = Country;
            item.PostalCode = PostalCode;
            item.Website = Website;
            item.Notes = Notes;
            item.CommercialRegister = CommercialRegister;
            item.TaxCard = TaxCard;
            item.Image = SaveImage(ImageSource);
        }

        private void Finish(object parameter)
        {
            Result = true;
            System.Windows.Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive)?.Close();
        }

        private void NotifyCustomersChangedIfNeeded()
        {
            if (typeof(T) == typeof(Customer))
            {
                App.NotifyCustomersChanged();
            }
        }
        #region UI State
        public enum DialogState { Add, Modify }
        private DialogState _currentState;
        public DialogState CurrentState
        {
            get { return _currentState; }
            set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    OnPropertyChanged(nameof(CurrentState));
                }
            }
        }
        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

