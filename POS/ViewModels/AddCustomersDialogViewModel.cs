using POS.Domain.Models;

namespace POS.ViewModels
{
    public class AddCustomersDialogViewModel : AddOrEditPersonViewModel<Customer>
    {
        protected override string ImageFolderName => "Customers";
    }
}

