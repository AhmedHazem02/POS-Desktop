using POS.Domain.Models;

namespace POS.ViewModels
{
    public class AddSuppliersDialogViewModel : AddOrEditPersonViewModel<Supplier>
    {
        protected override string ImageFolderName => "Suppliers";
    }
}

