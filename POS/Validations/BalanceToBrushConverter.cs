using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace POS.Validations
{
    public class BalanceToBrushConverter : IValueConverter
    {
        public Brush PositiveBrush { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
        public Brush NegativeBrush { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
        public Brush NeutralBrush { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return NeutralBrush;
            }

            if (decimal.TryParse(value.ToString(), out var amount))
            {
                if (amount > 0)
                {
                    return PositiveBrush;
                }

                if (amount < 0)
                {
                    return NegativeBrush;
                }
            }

            return NeutralBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
