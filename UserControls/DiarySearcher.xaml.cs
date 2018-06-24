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
using System.Collections.ObjectModel;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Diarix.UserControls
{
    public enum DiarySearchOption
    {
        AndSearch,
        OrSearch,
    }
    public sealed partial class DiarySearcher : UserControl, INotifyPropertyChanged
    {
        private bool _isAndSearchEnabled;
        private string _searchKeyword;
        private bool _isCompactModeEnabled;
        private string _searchResultExplanation;
        private bool isAndSearchEnabled
        {
            get => this._isAndSearchEnabled;
            set
            {
                if (this._isAndSearchEnabled != value)
                {
                    this._isAndSearchEnabled = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isAndSearchEnabled)));
                }
            }
        }
        private string searchKeyword
        {
            get => this._searchKeyword;
            set
            {
                if (this._searchKeyword != value)
                {
                    this._searchKeyword = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.searchKeyword)));
                }
            }
        }
        public bool IsCompactModeEnabled
        {
            get => this._isCompactModeEnabled;
            set
            {
                this._isCompactModeEnabled = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsCompactModeEnabled)));
            }
        }
        public string SearchResultExplanation
        {
            get => this._searchResultExplanation;
            set
            {
                this._searchResultExplanation = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SearchResultExplanation)));
            }
        }
        public ObservableCollection<KeywordSurrounding> KeywordSurrounding { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 検索ボタンがタップされたときに発生します．
        /// </summary>
        public event EventHandler<Tuple<IEnumerable<string>, DiarySearchOption>> SearchButtonTapped;
        /// <summary>
        /// 検索結果がタップされたときに発生します．
        /// </summary>
        public event EventHandler<DateTime> SearchResultTapped;
        public DiarySearcher()
        {
            this.InitializeComponent();
            this.isAndSearchEnabled = true;
            this.KeywordSurrounding = new ObservableCollection<KeywordSurrounding>();
            this.KeywordSurrounding.CollectionChanged += (sender, e) =>
            this.SearchResultExplanation = $"検索結果: {this.KeywordSurrounding.Count}件見つかりました．";
        }
        private void OnSearchRequested()
        {
            //半角および全角空白をキーワードの区切れ位置とみなして検索キーワードを取得
            var keywords = this.searchKeyword?.Split(' ', '　').Where(keyword => keyword.Any());
            //キーワードが指定されていなければ何もしない
            if (keywords == null || !keywords.Any()) return;
            //
            var option = this.isAndSearchEnabled ? DiarySearchOption.AndSearch : DiarySearchOption.OrSearch;
            this.SearchButtonTapped?.Invoke(this, new Tuple<IEnumerable<string>, DiarySearchOption>(keywords, option));
        }
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            switch ((sender as RadioButton)?.Tag)
            {
                case "And": this.isAndSearchEnabled = true; break;
                case "Or": this.isAndSearchEnabled = false; break;
                default: throw new ArgumentException("Unexpected sender was passed.");
            }
        }
        private void SearchButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.OnSearchRequested();
        }
        private void SearchButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.OnSearchRequested();
        }
        private void ListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if ((sender as ListView).SelectedItem is KeywordSurrounding item) this.SearchResultTapped?.Invoke(this, item.DateTime);
        }
        private void KeywordTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter) return;
            e.Handled = true;
            this.OnSearchRequested();
        }
    }
}
