using System.ComponentModel;
using System.Windows.Media;

namespace POS.ViewModels
{
    public class CashBillItem : INotifyPropertyChanged
    {
        private int _count;

        public CashBillItem(double value, string label, Brush background)
        {
            Value = value;
            Label = label;
            Background = background;
        }

        public double Value { get; }
        public string Label { get; }
        public Brush Background { get; }

        public int Count
        {
            get => _count;
            set
            {
                if (_count != value)
                {
                    _count = value;
                    OnPropertyChanged(nameof(Count));
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public bool IsSelected => Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
