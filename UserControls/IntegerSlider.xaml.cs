using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Diarix.UserControls
{
    namespace Converters
    {
        public class Int32ToDoubleConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language) => System.Convert.ToDouble(value);
            public object ConvertBack(object value, Type targetType, object parameter, string language) => System.Convert.ToInt32(value);
        }
    }
    public sealed partial class IntegerSlider : UserControl, INotifyPropertyChanged
    {
        private int _maximum;
        private int _minimum;
        private int _tickFrequency;
        public int Maximum
        {
            get => this._maximum;
            set
            {
                if (this._maximum != value)
                {
                    this._maximum = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Maximum)));
                }
            }
        }
        public int Minimum
        {
            get => this._minimum;
            set
            {
                if (this._minimum != value)
                {
                    this._minimum = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Minimum)));
                }
            }
        }
        public int TickFrequency
        {
            get => this._tickFrequency;
            set
            {
                if (this._tickFrequency != value)
                {
                    this._tickFrequency = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.TickFrequency)));
                }
            }
        }
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set
            {
                if (this.Value != value)
                {
                    SetValue(ValueProperty, value);
                }
            }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(int), typeof(IntegerSlider), null);
        public event PropertyChangedEventHandler PropertyChanged;
        public IntegerSlider()
        {
            this.InitializeComponent();
        }
    }
}
