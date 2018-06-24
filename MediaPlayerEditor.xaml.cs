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
    public sealed partial class MediaPlayerEditor : UserControl, INotifyPropertyChanged
    {
        private bool _isCompactModeEnabled;
        public bool IsCompactModeEnabled
        {
            get => this._isCompactModeEnabled;
            set
            {
                this._isCompactModeEnabled = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsCompactModeEnabled)));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler MediaDeleted;
        public MediaPlayerEditor()
        {
            this.InitializeComponent();
        }
        private void DeleteButton_Tapped(object sender, TappedRoutedEventArgs e) => this.MediaDeleted?.Invoke(this, EventArgs.Empty);
    }
}
