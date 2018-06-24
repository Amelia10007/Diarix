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
using Windows.ApplicationModel.Resources;
using Windows.UI;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Diarix.UserControls
{
    public sealed partial class SettingManager : UserControl, INotifyPropertyChanged
    {
        private bool _isCompactButtonEnabled;
        private bool _isSkipPasswordInputEnabled;
        private string _newPassword1, _newPassword2, _messageText;
        private Brush _passwordMessageBrush;
        private bool _isPasswordChangeOkButtonEnabled;
        public bool IsCompactButtonEnabled
        {
            get => this._isCompactButtonEnabled;
            set
            {
                if (this._isCompactButtonEnabled != value)
                {
                    this._isCompactButtonEnabled = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsCompactButtonEnabled)));
                }
            }
        }
        public bool IsSkipPasswordInputEnabled
        {
            get => this._isSkipPasswordInputEnabled;
            set
            {
                if (this._isSkipPasswordInputEnabled != value)
                {
                    this._isSkipPasswordInputEnabled = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsSkipPasswordInputEnabled)));
                }
            }
        }
        private string newPassword1
        {
            get => this._newPassword1;
            set
            {
                if (this._newPassword1 != value)
                {
                    this._newPassword1 = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.newPassword1)));
                }
            }
        }
        private string newPassword2
        {
            get => this._newPassword2;
            set
            {
                if (this._newPassword2 != value)
                {
                    this._newPassword2 = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.newPassword2)));
                }
            }
        }
        private string passwordMessageText
        {
            get => this._messageText;
            set
            {
                this._messageText = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.passwordMessageText)));
            }
        }
        private Brush passwordMessageBrush
        {
            get => this._passwordMessageBrush;
            set
            {
                this._passwordMessageBrush = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.passwordMessageBrush)));
            }
        }
        private bool isPasswordChangeOkButtonEnabled
        {
            get => this._isPasswordChangeOkButtonEnabled;
            set
            {
                this._isPasswordChangeOkButtonEnabled = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isPasswordChangeOkButtonEnabled)));
            }
        }
        public string NewPassword =>
            ((this.newPassword1?.Length ?? 0) > 0 && (this.newPassword2?.Length ?? 0) > 0 && this.newPassword1 == this.newPassword2) ? this.newPassword1 : null;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> CompactButtonToggled;
        public event EventHandler<bool> PasswordSkipToggled;
        public event EventHandler<string> NewPasswordDetermined;
        public SettingManager()
        {
            this.InitializeComponent();
            this.PropertyChanged += this.OnPropertyChanged;
        }
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //現在入力されているパスワードの状態に応じてメッセージを表示
            if (e.PropertyName == nameof(this.newPassword1) || e.PropertyName == nameof(this.newPassword2))
            {
                //両方ともnull or 0文字の場合
                if ((!this.newPassword1?.Any() ?? true) && (!this.newPassword2?.Any() ?? true))
                {
                    this.passwordMessageText = "パスワードは1文字以上に設定してください．";
                    this.passwordMessageBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, 0, 0));
                    this.isPasswordChangeOkButtonEnabled = false;
                }
                else if (this.newPassword1 != this.newPassword2)
                {
                    this.passwordMessageText = "上下のフォームに同じパスワードを入力してください．";
                    this.passwordMessageBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, 0, 0));
                    this.isPasswordChangeOkButtonEnabled = false;
                }
                else
                {
                    this.passwordMessageText = "適切なパスワードです．";
                    this.passwordMessageBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 0, byte.MaxValue, 0));
                    this.isPasswordChangeOkButtonEnabled = true;
                }
            }
        }
        private void CompactToggleSwitch_Toggled(object sender, RoutedEventArgs e) =>
            this.CompactButtonToggled?.Invoke(this, (sender as ToggleSwitch).IsOn);
        private void CompactToggleSwitch_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.IsCompactButtonEnabled ^= true;
        }
        private void PasswordSkipToggleSwitch_Toggled(object sender, RoutedEventArgs e) =>
            this.PasswordSkipToggled?.Invoke(this, (sender as ToggleSwitch).IsOn);
        private void PasswordSkipToggleSwitch_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.IsSkipPasswordInputEnabled ^= true;
        }
        private void PasswordBox1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this.newPassword1 = (sender as PasswordBox).Password;
        }
        private void PasswordBox2_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this.newPassword2 = (sender as PasswordBox).Password;
        }
        private void PasswordChangeButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.NewPasswordDetermined?.Invoke(this, this.newPassword1);
        }
        private void PasswordChangeButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.NewPasswordDetermined?.Invoke(this, this.newPassword1);
        }
    }
}
