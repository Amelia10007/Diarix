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
using Windows.Storage;
using System.Text;
using System.Security.Cryptography;
using System.ComponentModel;
using Windows.ApplicationModel.Resources;
using Windows.UI;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Diarix
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class StartPage : Page, INotifyPropertyChanged
    {
        private readonly Brush normalMessageBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 20, 150, 20));
        private readonly Brush errorMessageBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, byte.MaxValue, 0, 0));
        private bool _isOkButtonEnabled;
        private string _password;
        private bool? _isSkipPasswordInput;
        private string _message;
        private Brush _messageBrush;
        private bool isOkButtonEnabled
        {
            get => this._isOkButtonEnabled;
            set
            {
                this._isOkButtonEnabled = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isOkButtonEnabled)));
            }
        }
        private string password
        {
            get => this._password;
            set
            {
                if (this._password != value)
                {
                    this._password = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.password)));
                    //passwordが1文字以上に設定されていればOKボタンを有効にする
                    this.isOkButtonEnabled = value?.Any() ?? false;
                    //パスワードの入力状況に応じてメッセージを表示
                    if (this.isFirstAweken)
                    {
                        if (value?.Any() ?? false)
                        {
                            this.message = "適切なパスワードです．";
                            this.messageBrush = normalMessageBrush;
                        }
                        else
                        {
                            this.message = "パスワードは1文字以上に設定してください．";
                            this.messageBrush = errorMessageBrush;
                        }
                    }
                }
            }
        }
        private bool? isSkipPasswordInput
        {
            get => this._isSkipPasswordInput;
            set
            {
                if (this._isSkipPasswordInput != value)
                {
                    this._isSkipPasswordInput = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isSkipPasswordInput)));
                }
            }
        }
        private string message
        {
            get => this._message;
            set
            {
                this._message = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.message)));
            }
        }
        private Brush messageBrush
        {
            get => this._messageBrush;
            set
            {
                this._messageBrush = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.messageBrush)));
            }
        }
        private bool isFirstAweken;
        public event PropertyChangedEventHandler PropertyChanged;
        public StartPage()
        {
            this.InitializeComponent();
            this.isOkButtonEnabled = false;
            this.message = "Diarixへようこそ";
            this.messageBrush = normalMessageBrush;
            this.isSkipPasswordInput = false;
        }
        private async Task Enter()
        {
            if (this.password == null || !this.password.Any())
            {
                this.message = "パスワードは1文字以上に設定してください．";
                this.messageBrush = errorMessageBrush;
                return;
            }
            //初回起動時なら，設定を保存し，ハッシュを渡す
            if (this.isFirstAweken)
            {
                try
                {
                    var setting = await SettingFileset.OpenOrCreateSettingFilesetAsync();
                    setting.SetPassword(this.password);
                    setting.IsSkipPasswordInput = this.isSkipPasswordInput ?? false;
                    await setting.FlushAsync();
                }
                catch
                {
                    this.message = "設定の保存に失敗しました．";
                    this.messageBrush = errorMessageBrush;
                    return;
                }
            }
            //2回目以降の起動なら，設定ファイルに保存されたキーと入力されたパスワードを照合する
            else
            {
                try
                {
                    var setting = await SettingFileset.OpenOrCreateSettingFilesetAsync();
                    if (!setting.IsCorrectPassword(this.password))
                    {
                        this.message = "パスワードが間違っています．";
                        this.messageBrush = errorMessageBrush;
                        return;
                    }
                    //次回以降のパスワード入力をスキップするか保存
                    setting.IsSkipPasswordInput = this.isSkipPasswordInput ?? false;
                    await setting.FlushAsync();
                }
                catch
                {
                    this.message = "パスワードの認証に失敗しました．";
                    this.messageBrush = errorMessageBrush;
                    return;
                }
            }
            //メインパージへ移動
            this.Frame.Navigate(typeof(MainPage), nameof(StartPage));
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.isFirstAweken = !await SettingFileset.ExistSettingFilesetAsync();
            //設定ファイルがない場合は初回起動時用の処理
            if (this.isFirstAweken)
            {
                await new ContentDialog()
                {
                    Title = "Diarixへようこそ",
                    Content = "まずはパスワードを設定しましょう．\nパスワードは後から変更することもできます．",
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = "OK",
                }.ShowAsync();
            }
            //設定ファイルがある場合
            else
            {
                //設定ファイルを開き入力スキップ設定を取得．入力スキップがオンならメインページへ飛ぶ
                try
                {
                    var setting = await SettingFileset.OpenOrCreateSettingFilesetAsync();
                    this.isSkipPasswordInput = setting.IsSkipPasswordInput;
                    if (this.isSkipPasswordInput ?? false) Frame.Navigate(typeof(MainPage), nameof(StartPage));
                }
                catch { }
            }
        }
        private async void PasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) await this.Enter();
        }
        private void CheckBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.isSkipPasswordInput ^= true;
        }
        private async void OkButton_Tapped(object sender, RoutedEventArgs e) => await this.Enter();
        private async void OkButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) await this.Enter();
        }
    }
}
