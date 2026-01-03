using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace POS.Validations
{
    /// <summary>
    /// Converts a balance value to a color for modern UI badges
    /// Positive: Green, Negative: Red, Zero: Orange
    /// </summary>
    public class BalanceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Color.FromRgb(245, 158, 11); // Orange for neutral

            decimal balance;
            if (value is decimal decimalValue)
                balance = decimalValue;
            else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
                balance = parsedValue;
            else
                return Color.FromRgb(245, 158, 11);

            if (balance > 0)
                return Color.FromRgb(16, 185, 129); // Success Green #10B981
            else if (balance < 0)
                return Color.FromRgb(239, 68, 68); // Danger Red #EF4444
            else
                return Color.FromRgb(245, 158, 11); // Warning Orange #F59E0B
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
