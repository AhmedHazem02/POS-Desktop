using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace POS.Validations
{
    public class DoubleTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty; // Return empty to show placeholder
            }

            if (value is double number)
            {
                // Return empty string for 0 to show placeholder
                if (number == 0)
                {
                    return string.Empty;
                }
                return number.ToString("0.##", culture);
            }

            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = (value ?? string.Empty).ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0d;
            }

            var normalized = NormalizeNumber(text);
            if (double.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return 0d;
        }

        private static string NormalizeNumber(string input)
        {
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input.Trim())
            {
                if (ch >= '\u0660' && ch <= '\u0669')
                {
                    sb.Append((char)('0' + (ch - '\u0660')));
                    continue;
                }

                if (ch >= '\u06F0' && ch <= '\u06F9')
                {
                    sb.Append((char)('0' + (ch - '\u06F0')));
                    continue;
                }

                if (ch == ',' || ch == '٫')
                {
                    sb.Append('.');
                    continue;
                }

                sb.Append(ch);
            }

            return sb.ToString();
        }
    }
}
