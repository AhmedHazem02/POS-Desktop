using POS.Domain.Models.Payments.PaymentMethods;
using System;
using System.Globalization;
using System.Windows.Data;

namespace POS.Validations
{
    public class TransactionTypeToArabicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TransactionType type)
            {
                return type == TransactionType.Income ? "وارد" : "صادر";
            }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
