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
using Windows.UI;
using System.ComponentModel;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Diarix.UserControls
{
    namespace Converters
    {
        public sealed class ParagraphFontColorToGridBackgroundConverter : IValueConverter
        {
            public object Convert(object value, Type TargetType, object parameter, string language) =>
                new SolidColorBrush((value as Color?) ?? Color.FromArgb(byte.MaxValue, 0, 0, 0));
            public object ConvertBack(object value, Type TargetType, object parameter, string language) =>
                (value is SolidColorBrush) ? (value as SolidColorBrush).Color : Color.FromArgb(byte.MaxValue, 0, 0, 0);

        }
        public sealed class BooleanToBackgroundConverter : IValueConverter
        {
            public object Convert(object value, Type TargetType, object parameter, string language) =>
                new SolidColorBrush(Color.FromArgb((byte)(System.Convert.ToBoolean(value) ? 50 : 0), 0, 0, byte.MaxValue));
            public object ConvertBack(object value, Type TargetType, object parameter, string language) => throw new NotImplementedException();
        }
    }
    public sealed partial class ParagraphEditor : UserControl, INotifyPropertyChanged
    {
        private static readonly int paragraphFontSizeMin = Convert.ToInt32(Application.Current.Resources["ParagraphFontSizeMin"]);
        private static readonly int paragraphFontSizeMax = Convert.ToInt32(Application.Current.Resources["ParagraphFontSizeMax"]);
        private readonly int fontSizeTickFrequency;
        private bool _isCompactModeEnabled;
        private int _paragraphFontSize;
        private Color _paragraphFontColor;
        private bool _paragraphBold;
        private bool _paragraphItalic;
        public bool IsCompactModeEnabled
        {
            get => this._isCompactModeEnabled;
            set
            {
                this._isCompactModeEnabled = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsCompactModeEnabled)));
            }
        }
        public int ParagraphFontSize
        {
            get => this._paragraphFontSize;
            set
            {
                if (this._paragraphFontSize != value)
                {
                    this._paragraphFontSize = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ParagraphFontSize)));
                    this.ParagraphFontSizeChanged?.Invoke(this, this.ParagraphFontSize);
                }
            }
        }
        public Color ParagraphFontColor
        {
            get => this._paragraphFontColor;
            set
            {
                if (this._paragraphFontColor != value)
                {
                    this._paragraphFontColor = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ParagraphFontColor)));
                    this.ParagraphFontColorChanged?.Invoke(this, this.ParagraphFontColor);
                }
            }
        }
        public bool ParagraphBold
        {
            get => this._paragraphBold;
            set
            {
                if (this._paragraphBold != value)
                {
                    this._paragraphBold = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ParagraphBold)));
                    this.ParagraphBoldChanged?.Invoke(this, this.ParagraphBold);
                }
            }
        }
        public bool ParagraphItalic
        {
            get => this._paragraphItalic;
            set
            {
                if (this._paragraphItalic != value)
                {
                    this._paragraphItalic = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ParagraphItalic)));
                    this.ParagraphItalicChanged?.Invoke(this, this.ParagraphItalic);
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<int> ParagraphFontSizeChanged;
        public event EventHandler<Color> ParagraphFontColorChanged;
        public event EventHandler<bool> ParagraphBoldChanged;
        public event EventHandler<bool> ParagraphItalicChanged;
        public event EventHandler ParagraphDeleted;
        public ParagraphEditor() : this(Convert.ToInt32(Application.Current.Resources["ParagraphFontSizeDefault"]), (Color)Application.Current.Resources["ParagraphFontColorDefault"], false, false)
        {
        }
        public ParagraphEditor(int FontSize, Color FontColor, bool bold, bool italic)
        {
            this.InitializeComponent();
            this.fontSizeTickFrequency = Convert.ToInt32(this.Resources["FontSizeTickFrequency"]);
            this.ParagraphFontSize = FontSize;
            this.ParagraphFontColor = FontColor;
            this.ParagraphBold = bold;
            this.ParagraphItalic = italic;
        }
        private void ParagraphFontSizePrevious_Tapped(object sender, TappedRoutedEventArgs e) =>
            this.ParagraphFontSize = Math.Max(this.ParagraphFontSize - this.fontSizeTickFrequency, paragraphFontSizeMin);
        private void ParagraphFontSizePrevious_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                this.ParagraphFontSize = Math.Max(this.ParagraphFontSize - this.fontSizeTickFrequency, paragraphFontSizeMin);
        }
        private void ParagraphFontSizeNext_Tapped(object sender, TappedRoutedEventArgs e) =>
            this.ParagraphFontSize = Math.Min(this.ParagraphFontSize + this.fontSizeTickFrequency, paragraphFontSizeMax);
        private void ParagraphFontSizeNext_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                this.ParagraphFontSize = Math.Min(this.ParagraphFontSize + this.fontSizeTickFrequency, paragraphFontSizeMax);
        }
        private void ParagraphFontSizeDefault_Tapped(object sender, TappedRoutedEventArgs e) =>
            this.ParagraphFontSize = Convert.ToInt32(Application.Current.Resources["ParagraphFontSizeDefault"]);
        private void ParagraphFontSizeDefault_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                this.ParagraphFontSize = Convert.ToInt32(Application.Current.Resources["ParagraphFontSizeDefault"]);
        }
        private void BoldButton_Tapped(object sender, TappedRoutedEventArgs e) =>
            this.ParagraphBold ^= true;
        private void BoldButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.ParagraphBold ^= true;
        }
        private void ItalicButton_Tapped(object sender, TappedRoutedEventArgs e) =>
            this.ParagraphItalic ^= true;
        private void ItalicButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.ParagraphItalic ^= true;
        }
        private void ChangeColorToNextButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.ParagraphFontColor = CycleColorCollection.Next(this.ParagraphFontColor);
        }
        private void ChangeColorToNextButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                this.ParagraphFontColor = CycleColorCollection.Next(this.ParagraphFontColor);
        }
        private void ChangeColorToPreviousButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.ParagraphFontColor = CycleColorCollection.Previous(this.ParagraphFontColor);
        }
        private void ChangeColorToPreviousButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                this.ParagraphFontColor = CycleColorCollection.Previous(this.ParagraphFontColor);
        }
        private void ParagraphDeleteButton_Tapped(object sender, TappedRoutedEventArgs e) =>
            this.ParagraphDeleted?.Invoke(this, EventArgs.Empty);
        private void ParagraphDeleteButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.ParagraphDeleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
