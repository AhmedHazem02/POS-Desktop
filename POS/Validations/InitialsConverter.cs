using System;
using System.Globalization;
using System.Windows.Data;
using System.Linq;

namespace POS.Validations
{
    /// <summary>
    /// Converts a full name to initials for avatar display
    /// Example: "محمد أحمد" → "م أ"
    /// </summary>
    public class InitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return "؟";

            string name = value.ToString().Trim();
            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return "؟";

            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length));

            // Take first character from first two parts
            return string.Join(" ", parts.Take(2).Select(p => p.Substring(0, 1)));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
