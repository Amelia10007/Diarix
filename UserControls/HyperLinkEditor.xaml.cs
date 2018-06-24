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
using Windows.UI;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Diarix.UserControls
{
    public sealed partial class HyperLinkEditor : UserControl, INotifyPropertyChanged
    {
        private bool _isCompactModeEnabled;
        private string _navigateText;
        private Brush _navigateTextForeground;
        private string _explanationText;
        public bool IsCompactModeEnabled
        {
            get => this._isCompactModeEnabled;
            set
            {
                this._isCompactModeEnabled = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsCompactModeEnabled)));
            }
        }
        public string NavigateText
        {
            get => this._navigateText;
            set
            {
                if (this._navigateText != value)
                {
                    this._navigateText = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.NavigateText)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.NavigateUri)));
                    this.NavigateUriChanged?.Invoke(this, this.NavigateUri);
                    //
                    Color brushColor = Uri.IsWellFormedUriString(value, UriKind.Absolute) ?
                        Color.FromArgb(byte.MaxValue, 0, 0, 0) : Color.FromArgb(byte.MaxValue, byte.MaxValue, 0, 0);
                    this.navigateTextForeground = new SolidColorBrush(brushColor);
                }
            }
        }
        private Brush navigateTextForeground
        {
            get => this._navigateTextForeground;
            set
            {
                this._navigateTextForeground = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.navigateTextForeground)));
            }
        }
        public string ExplanationText
        {
            get => this._explanationText;
            set
            {
                if (this._explanationText != value)
                {
                    this._explanationText = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ExplanationText)));
                    this.ExplanationTextChanged?.Invoke(this, this.ExplanationText);
                }
            }
        }
        public Uri NavigateUri
        {
            get => Uri.IsWellFormedUriString(this.NavigateText, UriKind.Absolute) ? new Uri(this.NavigateText) : null;
            set
            {
                if (this.NavigateUri != value)
                {
                    this.NavigateText = value?.ToString();
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.NavigateUri)));
                    this.NavigateUriChanged?.Invoke(this, this.NavigateUri);
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<Uri> NavigateUriChanged;
        public event EventHandler<string> ExplanationTextChanged;
        public event EventHandler HyperLinkDeleted;
        public HyperLinkEditor()
        {
            this.InitializeComponent();
        }
        private void NavigateText_Changed(object sender, TextChangedEventArgs e) => this.NavigateText = (sender as TextBox).Text;
        private void ExplanationText_Changed(object sender, TextChangedEventArgs e) => this.ExplanationText = (sender as TextBox).Text;
        public void DeleteButton_Tapped(object sender, TappedRoutedEventArgs e) => this.HyperLinkDeleted?.Invoke(this, null);
        private void DeleteButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.HyperLinkDeleted?.Invoke(this, null);
        }
    }
}
