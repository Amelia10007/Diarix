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
using System.Threading.Tasks;
using System.ComponentModel;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Diarix.ContentDialogs
{
    public enum CheckToSaveDialyDialogResult
    {
        None,
        Save,
        DontSave,
        Cancel,
    }
    public sealed partial class CheckToSaveDiaryDialog : ContentDialog,INotifyPropertyChanged
    {
        private Visibility _cancelButtonVisibility;
        public Visibility CancelButtonVisibility
        {
            get => this._cancelButtonVisibility;
            set
            {
                this._cancelButtonVisibility = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.CancelButtonVisibility)));
            }
        }
        public CheckToSaveDialyDialogResult Result { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public CheckToSaveDiaryDialog(bool isCancelButtonEnable)
        {
            this.InitializeComponent();
            this.CancelButtonVisibility = isCancelButtonEnable ? Visibility.Visible : Visibility.Collapsed;
            this.Result = CheckToSaveDialyDialogResult.None;
        }
        public new async Task<CheckToSaveDialyDialogResult> ShowAsync()
        {
            await base.ShowAsync();
            return this.Result;
        }
        private void SaveButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Result = CheckToSaveDialyDialogResult.Save;
            this.Hide();
        }
        private void SaveButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter) return;
            this.Result = CheckToSaveDialyDialogResult.Save;
            this.Hide();
        }
        private void DontSaveButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Result = CheckToSaveDialyDialogResult.DontSave;
            this.Hide();
        }
        private void DontSaveButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter) return;
            this.Result = CheckToSaveDialyDialogResult.DontSave;
            this.Hide();
        }
        private void CancelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Result = CheckToSaveDialyDialogResult.Cancel;
            this.Hide();
        }
        private void CancelButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter) return;
            this.Result = CheckToSaveDialyDialogResult.Cancel;
            this.Hide();
        }
    }
}
