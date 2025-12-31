using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace POS.Validations
{
    public class DecimalTextBoxBehavior
    {
        public static readonly DependencyProperty AllowDecimalPointProperty =
            DependencyProperty.RegisterAttached("AllowDecimalPoint", typeof(bool), typeof(DecimalTextBoxBehavior), new PropertyMetadata(false, OnAllowDecimalPointChanged));

        public static bool GetAllowDecimalPoint(TextBox textBox)
        {
            return (bool)textBox.GetValue(AllowDecimalPointProperty);
        }

        public static void SetAllowDecimalPoint(TextBox textBox, bool value)
        {
            textBox.SetValue(AllowDecimalPointProperty, value);
        }

        private static void OnAllowDecimalPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += TextBox_PreviewTextInput;
                    DataObject.AddPastingHandler(textBox, TextBox_Pasting);
                }
                else
                {
                    textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                    DataObject.RemovePastingHandler(textBox, TextBox_Pasting);
                }
            }
        }

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (!IsValidInput(textBox.Text, e.Text, GetAllowDecimalPoint(textBox)))
            {
                e.Handled = true;
            }
        }

        private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            var pasteText = (string)e.DataObject.GetData(typeof(string)) ?? string.Empty;
            var textBox = (TextBox)sender;
            if (!IsValidInput(textBox.Text, pasteText, GetAllowDecimalPoint(textBox)))
            {
                e.CancelCommand();
            }
        }

        private static bool IsValidInput(string existingText, string newText, bool allowDecimalPoint)
        {
            foreach (var ch in newText)
            {
                // Allow digits (including Arabic-Indic numerals)
                if (char.IsDigit(ch) || IsArabicDigit(ch))
                {
                    continue;
                }

                if (allowDecimalPoint && IsDecimalSeparator(ch))
                {
                    if (ContainsDecimalSeparator(existingText))
                    {
                        return false;
                    }

                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool IsArabicDigit(char ch)
        {
            // Arabic-Indic digits: ٠١٢٣٤٥٦٧٨٩
            return ch >= '\u0660' && ch <= '\u0669';
        }

        private static bool ContainsDecimalSeparator(string text)
        {
            return text.Contains(".") || text.Contains(",") || text.Contains("٫");
        }

        private static bool IsDecimalSeparator(char ch)
        {
            return ch == '.' || ch == ',' || ch == '٫';
        }
    }
}
