using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace POS.Validations
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Check if parameter is "inverse" to invert the boolean
                bool isInverse = parameter?.ToString()?.ToLower() == "inverse";
                bool result = isInverse ? !boolValue : boolValue;
                
                return result ? Visibility.Visible : Visibility.Collapsed;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                bool isInverse = parameter?.ToString()?.ToLower() == "inverse";
                bool result = visibility == Visibility.Visible;
                return isInverse ? !result : result;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
